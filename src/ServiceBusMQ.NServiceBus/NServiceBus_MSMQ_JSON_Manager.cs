#region File Information
/********************************************************************
  Project: ServiceBusMQ.NServiceBus
  File:    NServiceBus_MSMQ_JSON_Manager.cs
  Created: 2013-01-26

  Author(s):
    Daniel Halan

 (C) Copyright 2013 Ingenious Technology with Quality Sweden AB
     all rights reserved

********************************************************************/
#endregion

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using NServiceBus;
using ServiceBusMQ.Manager;
using ServiceBusMQ.Model;

namespace ServiceBusMQ.NServiceBus {
  public class NServiceBus_MSMQ_JSON_Manager : NServiceBus_MSMQ_Manager {

    public override string TransportationName { get { return "MSMQ (JSON)"; } }

    public override void Initialize(string serverName, Queue[] monitorQueues) {
      base.Initialize(serverName, monitorQueues);

      //_ignoreMessageBody = new StreamReader(this.GetType().Assembly.GetManifestResourceStream("ServiceBusMQ.NServiceBus.CompletionMessage.json")).ReadToEnd();
    }


    public override void SetupServiceBus(string[] assemblyPaths, CommandDefinition cmdDef) {
      _commandDef = cmdDef;

      List<Assembly> asms = new List<Assembly>();

      foreach( string path in assemblyPaths ) {

        foreach( string file in Directory.GetFiles(path, "*.dll") ) {
          try {
            asms.Add(Assembly.LoadFrom(file));
          } catch { }
        }

      }


      _bus = Configure.With(asms)
                .DefineEndpointName("SBMQM_NSB_JSON")
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
    public override object DeserializeCommand(string cmd, Type cmdType) {
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
