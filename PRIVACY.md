# Privacy

ThermoWhisper works offline.

- The app makes no network calls. It has no telemetry, no analytics, no crash reporting and no accounts.
- The app stores only these values, on this computer:
  - the quiet profile (the CPU % you chose in Settings);
  - the UI language;
  - the first-run date;
  - the donation-reminder flag;
  - the donation link (`DonationUrl` in SecureStorage).
- The app does not send any of these values anywhere.
- Autostart: if you turn it on, the app registers itself as a Windows startup item. This setting is kept by Windows, not by the app; the app reads it and changes it only when you use the Settings window.
- The donation reminder is a local Windows notification, shown once. It does not use the network.
- The "Donate" button only opens a web page in your browser when you click it. Nothing is sent by the app before that.
- The app reads and writes the maximum processor state of the active Windows power scheme. It does not read any other personal data.
