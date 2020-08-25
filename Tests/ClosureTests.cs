﻿using System.Collections.Generic;
using Speckle.Core.Transports;
using NUnit.Framework;
using Speckle.Core.Models;
using Speckle.Core.Api;
using Newtonsoft.Json;

namespace Tests
{
  [TestFixture]
  public class Closures
  {

    [Test(Description = "Checks wether closures are generated correctly by the serialiser.")]
    public void CorrectDecompositionTracking()
    {
      var d5 = new Base();
      ((dynamic)d5).name = "depth five"; // end v

      var d4 = new Base();
      ((dynamic)d4).name = "depth four";
      ((dynamic)d4)["@detach"] = d5;

      var d3 = new Base();
      ((dynamic)d3).name = "depth three";
      ((dynamic)d3)["@detach"] = d4;

      var d2 = new Base();
      ((dynamic)d2).name = "depth two";
      ((dynamic)d2)["@detach"] = d3;
      ((dynamic)d2)["@joker"] = new object[] { d5 };

      var d1 = new Base();
      ((dynamic)d1).name = "depth one";
      ((dynamic)d1)["@detach"] = d2;
      ((dynamic)d1)["@joker"] = d5; // consequently, d5 depth in d1 should be 1

      var transport = new MemoryTransport();

      var result = Operations.Send(d1, new List<ITransport>() { transport }, false).Result;

      var test = Operations.Receive(result, transport).Result;

      Assert.IsNotNull(test.id);
      Assert.AreEqual(test.id, d1.GetId(true));

      var d1_ = JsonConvert.DeserializeObject<dynamic>(transport.Objects[d1.GetId(true)]);
      var d2_ = JsonConvert.DeserializeObject<dynamic>(transport.Objects[d2.GetId(true)]);
      var d3_ = JsonConvert.DeserializeObject<dynamic>(transport.Objects[d3.GetId(true)]);
      var d4_ = JsonConvert.DeserializeObject<dynamic>(transport.Objects[d4.GetId(true)]);
      var d5_ = JsonConvert.DeserializeObject<dynamic>(transport.Objects[d5.GetId(true)]);


      var depthOf_d5_in_d1 = int.Parse((string)d1_.__closure[d5.GetId(true)]);
      Assert.AreEqual(1, depthOf_d5_in_d1);

      var depthOf_d4_in_d1 = int.Parse((string)d1_.__closure[d4.GetId(true)]);
      Assert.AreEqual(3, depthOf_d4_in_d1);

      var depthOf_d5_in_d3 = int.Parse((string)d3_.__closure[d5.GetId(true)]);
      Assert.AreEqual(2, depthOf_d5_in_d3);

      var depthOf_d4_in_d3 = int.Parse((string)d3_.__closure[d4.GetId(true)]);
      Assert.AreEqual(1, depthOf_d4_in_d3);

      var depthOf_d5_in_d2 = int.Parse((string)d2_.__closure[d5.GetId(true)]);
      Assert.AreEqual(1, depthOf_d5_in_d2);
    }

  }
}
