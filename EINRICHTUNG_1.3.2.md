# MAC 1.3.2 – Updates und automatisch schließendes Setup

Der Anmeldeserver bleibt unverändert. Kein Render-Deploy, keine Änderung von MAC_ADMIN_HASH oder Beraterkonten erforderlich.

1. Das Administrator-ZIP entpacken. Den Inhalt von `Quellcode/MAC-App` in die Wurzel von `Tunfisch77/MAC` übernehmen und committen. Keine Kundendateien, Passwörter oder lokalen Entwürfe hochladen.
2. Öffentliches reguläres GitHub Release `v1.3.2` erstellen und als neuestes Release markieren. Kein Draft oder Pre-release.
3. Genau die drei Dateien aus `Release-Dateien` unverändert anhängen: `MAC-Setup-1.3.2.exe`, `MAC-Programm-1.3.2.zip`, `SHA256SUMS.txt`. Programm-ZIP nicht entpacken oder neu packen, da das Setup seine genaue Prüfsumme enthält. Dateien aus unterschiedlichen Builds nicht mischen.
4. Release veröffentlichen. Erst jetzt kann das neue Setup seine Programmdateien laden.
5. Offene Eingaben exportieren. In der bisherigen App „Nach Updates suchen“ anklicken oder MAC-Setup-1.3.2.exe starten. Die automatische Suche ist erst ab der installierten Version 1.3.2 aktiv.
6. Nach erfolgreicher Installation startet die App; das Setup schließt sich automatisch, sobald der App-Start bestätigt ist. Bei Fehlern oder nicht erstellten Verknüpfungen bleibt es offen.

## Künftige Updates

Für jede Änderung eine höhere Versionsnummer in src/Version.cs setzen und mit build.ps1 neu bauen. Die drei zusammengehörigen Dateien in einem neuen regulären Release mit passendem v-Tag veröffentlichen. Bereits veröffentlichte Pakete nicht unter derselben Versionsnummer austauschen. Die App prüft etwa stündlich, während sie geöffnet ist, auch minimiert. Ein zusätzlicher Windows-Dienst oder Autostart wird nicht installiert. Der erste Check erfolgt kurz nach dem Öffnen eines angemeldeten Profils; während Anmeldung, Sperre und Dialogen wird gewartet.

„Später“ unterdrückt dieselbe Version für 24 Stunden, auch nach Neustart. Die Einstellung ist lokal pro Windows-Benutzer, keine kundenbezogenen Daten. Manuelle Prüfung bleibt möglich. Update-Download und Installation erfolgen nur nach Bestätigung; vor der Installation müssen offene Eingaben als Excel exportiert werden. Kein Excel-Speichern = kein Update-Start. Kundendaten werden nicht an GitHub übertragen.

## Bauen und Prüfen

Windows-Build: powershell -NoProfile -ExecutionPolicy Bypass -File .\build.ps1

Tests für Zeitplan und gespeicherte Zurückstellung sind im App-Selbsttest enthalten. tests/UpdateUiSmoke.cs und tests/InstallerUiSmoke.cs prüfen die echten Dialog-/Fensterabläufe mit synthetischen Daten und ohne GitHub-Download. Native Windows-Abnahme: tests/WINDOWS.md. Der Testbericht nennt die tatsächlich ausgeführten Prüfungen und Grenzen.
