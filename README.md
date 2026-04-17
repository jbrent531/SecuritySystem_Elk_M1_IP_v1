# SecuritySystem_Elk_M1_IP_v1

A Crestron Home driver for the **ELK M1 Security System** over IP.

## Overview

This driver integrates the ELK M1 alarm panel with the Crestron Home platform,
exposing areas, zones, and keypads so users can arm/disarm, view status, and
interact with their security system from Crestron Home.

## Project structure

- `SecuritySystemDriverIP.cs` — main driver entry point (IP transport)
- `SecuritySystemProtocol.cs` — ELK M1 serial protocol implementation
- `SecuritySystemArea.cs` — area (partition) model and arm/disarm logic
- `SecuritySystemZone.cs` — zone model and state handling
- `SecuritySystemKeypad.cs` — keypad model
- `SampleTransport.cs` / `SendTransportData.cs` — transport helpers
- `SecuritySystemDriver.json` — driver manifest consumed by Crestron Home

## Build

Open `SecuritySystem_Elk_M1_IP_v1.sln` in Visual Studio. The project targets
**.NET Framework 4.7.2** and references the Crestron device driver SDK
(`Crestron.DeviceDrivers.API` / `Crestron.DeviceDrivers.Core`) via NuGet.

Restore packages and build — the compiled DLL and `SecuritySystemDriver.json`
make up the driver package installed on the Crestron Home processor.

## Status

Work in progress.
