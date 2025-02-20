﻿using MQTTnet.Client;
using MQTTnet.Extensions.MultiCloud.AzureIoTClient.TopicBindings;
using System;
using System.Text.Json.Nodes;
using System.Threading;
using System.Threading.Tasks;

namespace MQTTnet.Extensions.MultiCloud.AzureIoTClient
{
    public class HubMqttClient : IHubMqttClient
    {
        public IMqttClient Connection { get; set; }

        public string InitialState { get; set; } = String.Empty;

        private readonly IPropertyStoreReader getTwinBinder;
        private readonly IPropertyStoreWriter updateTwinBinder;
        private readonly GenericDesiredUpdatePropertyBinder genericDesiredUpdateProperty;
        private readonly GenericCommand command;

        public HubMqttClient(IMqttClient c)
        {
            Connection = c;
            getTwinBinder = new GetTwinBinder(c);
            updateTwinBinder = new UpdateTwinBinder(c);
            command = new GenericCommand(c);
            genericDesiredUpdateProperty = new GenericDesiredUpdatePropertyBinder(c, updateTwinBinder);
        }

        public async Task InitState()
        {
            await command.InitSubscriptionsAsync();
            await genericDesiredUpdateProperty.InitiSubscriptionsAsync();
            InitialState = await GetTwinAsync();
        }

        public Func<GenericCommandRequest, GenericCommandResponse> OnCommandReceived
        {
            get => command.OnCmdDelegate;
            set => command.OnCmdDelegate = value;
        }

        public Func<JsonNode, GenericPropertyAck> OnPropertyUpdateReceived
        {
            get => genericDesiredUpdateProperty.OnProperty_Updated;
            set => genericDesiredUpdateProperty.OnProperty_Updated = value;
        }

        public Task<string> GetTwinAsync(CancellationToken cancellationToken = default) => getTwinBinder.ReadPropertiesDocAsync(cancellationToken);
        public Task<int> ReportPropertyAsync(object payload, CancellationToken cancellationToken = default) => updateTwinBinder.ReportPropertyAsync(payload, cancellationToken);
        public Task<MqttClientPublishResult> SendTelemetryAsync(object payload, CancellationToken t = default) => Connection.PublishStringAsync($"devices/{Connection.Options.ClientId}/messages/events/", Json.Stringify(payload), Protocol.MqttQualityOfServiceLevel.AtLeastOnce, false, t);
        public Task<MqttClientPublishResult> SendTelemetryAsync(object payload, string componentName, CancellationToken t = default) => Connection.PublishStringAsync($"devices/{Connection.Options.ClientId}/messages/events/?$.sub={componentName}", Json.Stringify(payload), Protocol.MqttQualityOfServiceLevel.AtLeastOnce, false, t);

    }
}
