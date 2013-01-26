﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using NServiceBus;
using ServiceBusMQ.Manager;

namespace ServiceBusMQ.NServiceBus {
  public class NServiceBus_MSMQ_JSON_Manager : NServiceBus_MSMQ_Manager {

    public override string BusQueueType { get { return "MSMQ (JSON)"; } }

    public override void SetupBus(string[] assemblyPaths) {

      List<Assembly> asms = new List<Assembly>();

      foreach( string path in assemblyPaths ) {

        foreach( string file in Directory.GetFiles(path, "*.dll") ) {
          try {
            asms.Add(Assembly.LoadFrom(file));
          } catch { }
        }

      }


      _bus = Configure.With(asms)
                .DefineEndpointName("SBMQM_NSB")
                .DefaultBuilder()
        //.MsmqSubscriptionStorage()
          .DefiningCommandsAs(t => _commandDef.IsCommand(t))
                .JsonSerializer()
                .MsmqTransport()
                .UnicastBus()
            .SendOnly();

    }

    public override string SerializeCommand(object cmd) {

      var types = new List<Type> { cmd.GetType() };

      var mapper = new global::NServiceBus.MessageInterfaces.MessageMapper.Reflection.MessageMapper();
      mapper.Initialize(types);

      var serializr = new global::NServiceBus.Serializers.Json.JsonMessageSerializer(mapper);

      using( Stream stream = new MemoryStream() ) {
        serializr.Serialize(new[] { cmd }, stream);
        stream.Position = 0;

        return new StreamReader(stream).ReadToEnd();
      }

    }
    public override object DeserializeCommand(string cmd) {
      var types = new List<Type> { cmd.GetType() };

      var mapper = new global::NServiceBus.MessageInterfaces.MessageMapper.Reflection.MessageMapper();
      mapper.Initialize(types);

      var serializr = new global::NServiceBus.Serializers.Json.JsonMessageSerializer(mapper);

      using( Stream stream = new MemoryStream(Encoding.UTF8.GetBytes(cmd)) ) {
        var obj = serializr.Deserialize(stream);

        return obj[0];
      }

    }

    public override MessageContentFormat MessageContentFormat { get { return Manager.MessageContentFormat.Json; } }

  }
}
