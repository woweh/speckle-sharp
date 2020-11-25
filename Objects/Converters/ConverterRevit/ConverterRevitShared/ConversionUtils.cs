﻿using Autodesk.Revit.DB;
using Objects.Revit;
using Speckle.Core.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using DB = Autodesk.Revit.DB;

namespace Objects.Converter.Revit
{
  public partial class ConverterRevit
  {
    #region parameters

    private void AddCommonRevitProps(IBaseRevitElement speckleElement, DB.Element revitElement)
    {
      if (speckleElement is IRevitHasFamilyAndType foo)
      {
        foo.family = (revitElement as DB.FamilyInstance)?.Symbol?.FamilyName;
      }

      if (speckleElement is IRevitHasTypeParameters bar)
      {
        bar.typeParameters = GetElementTypeParams(revitElement);
      }

      if (speckleElement is IRevitHasParameters baz)
      {
        baz.parameters = GetElementParams(revitElement);
      }

      speckleElement.elementId = revitElement.Id.ToString();
      ((Base)speckleElement).applicationId = revitElement.UniqueId;
    }

    public Dictionary<string, object> GetElementParams(DB.Element myElement)
    {
      var myParamDict = new Dictionary<string, object>();

      // Get params from the unique list
      foreach (Parameter p in myElement.ParametersMap)
      {
        var keyName = SanitizeKeyname(p.Definition.Name);
        switch (p.StorageType)
        {
          case StorageType.Double:
            // NOTE: do not use p.AsDouble() as direct input for unit utils conversion, it doesn't work.  ¯\_(ツ)_/¯
            var val = p.AsDouble();
            try
            {
              myParamDict[keyName] = UnitUtils.ConvertFromInternalUnits(val, p.DisplayUnitType);
              myParamDict["__unitType::" + keyName] = p.Definition.UnitType.ToString();
              myParamDict["__unit::" + keyName] = p.DisplayUnitType.ToString();

            }
            catch (Exception)
            {
              myParamDict[keyName] = val;
            }
            break;
          case StorageType.Integer:
            myParamDict[keyName] = p.AsInteger();
            //myParamDict[ keyName ] = UnitUtils.ConvertFromInternalUnits( p.AsInteger(), p.DisplayUnitType);
            break;
          case StorageType.String:
            myParamDict[keyName] = p.AsString();
            break;
          case StorageType.ElementId:
            // TODO: (OLD) Properly get ref elemenet and serialise it in here.
            // NOTE: Too much garbage for too little info...
            //var docEl = Doc.GetElement( p.AsElementId() );
            //var spk = SpeckleCore.Converter.Serialise( docEl );
            //if( !(spk is SpeckleNull) ) {
            //  myParamDict[ keyName + "_el" ] = spk;
            //  myParamDict[ keyName ] = p.AsValueString();
            //} else
            myParamDict[keyName] = p.AsValueString();
            break;
          case StorageType.None:
            break;
        }
      }

      // Get any other parameters from the "big" list
      foreach (Parameter p in myElement.Parameters)
      {
        var keyName = SanitizeKeyname(p.Definition.Name);

        if (myParamDict.ContainsKey(keyName))
        {
          continue;
        }

        switch (p.StorageType)
        {
          case StorageType.Double:
            // NOTE: do not use p.AsDouble() as direct input for unit utils conversion, it doesn't work.  ¯\_(ツ)_/¯
            var val = p.AsDouble();
            try
            {
              myParamDict[keyName] = UnitUtils.ConvertFromInternalUnits(val, p.DisplayUnitType);
              myParamDict["__unitType::" + keyName] = p.Definition.UnitType.ToString();
              myParamDict["__unit::" + keyName] = p.DisplayUnitType.ToString();
            }
            catch (Exception)
            {
              myParamDict[keyName] = val;
            }
            break;
          case StorageType.Integer:
            myParamDict[keyName] = p.AsInteger();
            //myParamDict[ keyName ] = UnitUtils.ConvertFromInternalUnits( p.AsInteger(), p.DisplayUnitType);
            break;
          case StorageType.String:
            myParamDict[keyName] = p.AsString();
            break;
          case StorageType.ElementId:
            myParamDict[keyName] = p.AsValueString();
            break;
          case StorageType.None:
            break;
        }
      }

      //sort parameters
      myParamDict = myParamDict.OrderBy(obj => obj.Key).ToDictionary(obj => obj.Key, obj => obj.Value);


      // myParamDict["__units"] = unitsDict;
      // TODO: (OLD) BIG CORE PROBLEM: failure to serialise things with nested dictionary (like the line above).
      return myParamDict;
    }

    public Dictionary<string, object> GetElementTypeParams(DB.Element myElement)
    {
      var myElementType = Doc.GetElement(myElement.GetTypeId());

      if (myElementType == null || myElementType.Parameters == null) return null;

      var myParamDict = new Dictionary<string, object>();

      foreach (Parameter p in myElementType.Parameters)
      {
        var keyName = SanitizeKeyname(p.Definition.Name);

        if (myParamDict.ContainsKey(keyName))
        {
          continue;
        }

        switch (p.StorageType)
        {
          case StorageType.Double:
            // NOTE: do not use p.AsDouble() as direct input for unit utils conversion, it doesn't work.  ¯\_(ツ)_/¯
            var val = p.AsDouble();
            try
            {
              myParamDict[keyName] = UnitUtils.ConvertFromInternalUnits(val, p.DisplayUnitType);
              myParamDict["__unitType::" + keyName] = p.Definition.UnitType.ToString();
              myParamDict["__unit::" + keyName] = p.DisplayUnitType.ToString();
            }
            catch (Exception)
            {
              myParamDict[keyName] = val;
            }
            break;
          case StorageType.Integer:
            myParamDict[keyName] = p.AsInteger();
            break;
          case StorageType.String:
            myParamDict[keyName] = p.AsString();
            break;
          case StorageType.ElementId:
            myParamDict[keyName] = p.AsValueString();
            break;
          case StorageType.None:
            break;
        }
      }

      //sort parameters
      myParamDict = myParamDict.OrderBy(obj => obj.Key).ToDictionary(obj => obj.Key, obj => obj.Value);


      // myParamDict["__units"] = unitsDict;
      // TODO: (OLD) BIG CORE PROBLEM: failure to serialise things with nested dictionary (like the line above).
      return myParamDict;
    }

    public void SetElementParamsFromSpeckle(DB.Element myElement, IRevitHasParameters spkElement, List<string> exclusions = null)
    {
      if (myElement == null)
      {
        return;
      }

      if (spkElement.parameters == null)
      {
        return;
      }

      foreach (var kvp in spkElement.parameters)
      {
        if (kvp.Key.Contains("__unitType::"))
        {
          continue; // skip unit types please
        }

        if (exclusions != null && exclusions.Contains(kvp.Key))
        {
          continue;
        }

        try
        {
          var keyName = UnsanitizeKeyname(kvp.Key);

          //TODO: try support params in foreign language
          var myParam = myElement.ParametersMap.get_Item(keyName);
          if (myParam == null)
          {
            continue;
          }

          if (myParam.IsReadOnly)
          {
            continue;
          }

          switch (myParam.StorageType)
          {
            case StorageType.Double:
              var hasUnitKey = spkElement.parameters.ContainsKey("__unitType::" + myParam.Definition.Name);
              if (hasUnitKey)
              {
                var unitType = (string)spkElement.parameters["__unitType::" + kvp.Key];
                var unit = (string)spkElement.parameters["__unit::" + kvp.Key];
                DisplayUnitType sourceUnit;
                Enum.TryParse(unit, out sourceUnit);
                var convertedValue = UnitUtils.ConvertToInternalUnits(Convert.ToDouble(kvp.Value), sourceUnit);

                myParam.Set(convertedValue);
              }
              else
              {
                myParam.Set(Convert.ToDouble(kvp.Value));
              }
              break;

            case StorageType.Integer:
              myParam.Set(Convert.ToInt32(kvp.Value));
              break;

            case StorageType.String:
              myParam.Set(Convert.ToString(kvp.Value));
              break;

            case StorageType.ElementId:
              // TODO (OLD) /Fake out: most important element id params should go as props in the object model
              break;
          }
        }
        catch (Exception)
        {
        }
      }

    }

    private void TrySetParam(DB.Element elem, BuiltInParameter bip, DB.Element value)
    {
      var param = elem.get_Parameter(bip);
      if (param != null && value != null && !param.IsReadOnly)
      {
        param.Set(value.Id);
      }
    }

    public static string SanitizeKeyname(string keyName)
    {
      return keyName.Replace(".", "☞"); // BECAUSE FML
    }

    public static string UnsanitizeKeyname(string keyname)
    {
      return keyname.Replace("☞", ".");
    }

    #endregion

    #region  element types

    private T GetElementType<T>(string family, string type)
    {
      List<ElementType> types = new FilteredElementCollector(Doc).WhereElementIsElementType().OfClass(typeof(T)).ToElements().Cast<ElementType>().ToList();

      //match family and type
      var match = types.FirstOrDefault(x => x.FamilyName == family && x.Name == type);
      if (match != null)
      {
        if (match is FamilySymbol fs && !fs.IsActive)
        {
          fs.Activate();
        }

        return (T)(object)match;
      }

      //match family
      match = types.FirstOrDefault(x => x.FamilyName == family);
      if (match != null)
      {
        ConversionErrors.Add(new Error($"Missing type: {family} {type}", $"Type was replace with: {match.FamilyName} - {match.Name}"));
        if (match != null)
        {
          if (match is FamilySymbol fs && !fs.IsActive)
          {
            fs.Activate();
          }

          return (T)(object)match;
        }
      }

      // get whatever we found, could be a different category!
      if (types.Any())
      {
        match = types.FirstOrDefault();
        ConversionErrors.Add(new Error($"Missing family and type", $"The following family and type were used: {match.FamilyName} - {match.Name}"));
        if (match != null)
        {
          if (match is FamilySymbol fs && !fs.IsActive)
          {
            fs.Activate();
          }

          return (T)(object)match;
        }
      }

      throw new Exception($"Could not find any family symbol to use.");
    }

    private T GetElementType<T>(Base element)
    {
      List<ElementType> types = new List<ElementType>();
      ElementMulticategoryFilter filter = null;

      if (element is IColumn)
      {
        filter = new ElementMulticategoryFilter(Categories.columnCategories);
      }
      else if (element is IBeam || element is IBrace)
      {
        filter = new ElementMulticategoryFilter(Categories.beamCategories);
      }
      //else if (element is IDuct)
      //{
      //  filter = new ElementMulticategoryFilter(Categories.ductCategories);
      //}

      if (filter != null)
      {
        types = new FilteredElementCollector(Doc).WhereElementIsElementType().OfClass(typeof(T)).WherePasses(filter).ToElements().Cast<ElementType>().ToList();
      }
      else
      {
        types = new FilteredElementCollector(Doc).WhereElementIsElementType().OfClass(typeof(T)).ToElements().Cast<ElementType>().ToList();
      }


      if (element is IRevitHasFamilyAndType ire)
      {
        //match family and type
        var match = types.FirstOrDefault(x => x.FamilyName == ire.family && x.Name == ire.type);
        if (match != null)
        {
          if (match is FamilySymbol fs && !fs.IsActive)
          {
            fs.Activate();
          }

          return (T)(object)match;
        }


        //match type
        match = types.FirstOrDefault(x => x.FamilyName == ire.family);
        if (match != null)
        {
          ConversionErrors.Add(new Error($"Missing type: {ire.family} {ire.type}", $"Type was replace with: {match.FamilyName} - {match.Name}"));
          if (match != null)
          {
            if (match is FamilySymbol fs && !fs.IsActive)
            {
              fs.Activate();
            }

            return (T)(object)match;
          }
        }
      }

      // get whatever we found, could be a different category!
      if (types.Any())
      {
        var match = types.FirstOrDefault();
        ConversionErrors.Add(new Error($"Missing family and type", $"The following family and type were used: {match.FamilyName} - {match.Name}"));
        if (match != null)
        {
          if (match is FamilySymbol fs && !fs.IsActive)
          {
            fs.Activate();
          }

          return (T)(object)match;
        }
      }

      throw new Exception($"Could not find any family symbol to use.");
    }

    #endregion

    #region conversion "edit existing if possible" utilities
    /// <summary>
    /// Returns, if found, the corresponding doc element and its corresponding local state object.
    /// The doc object can be null if the user deleted it. 
    /// </summary>
    /// <param name="applicationId"></param>
    /// <returns></returns>
    public DB.Element GetExistingElementByApplicationId(string applicationId)
    {
      var @ref = ContextObjects.FirstOrDefault(o => o.applicationId == applicationId);

      if (@ref == null) return null;

      var docElement = Doc.GetElement(@ref.ApplicationGeneratedId);

      if (docElement != null)
      {
        return docElement;
      }

      return null;
    }

    #endregion

  }
}
