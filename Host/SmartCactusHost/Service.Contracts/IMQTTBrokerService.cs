﻿namespace Service.Contracts;

public interface IMQTTBrokerService
{
    void StartBroker();
    void StopBroker();
    string RequestDeviceCreation();
    bool IsStarted();
    void Ping();
    Task Publish(string topic, string payload);
}
