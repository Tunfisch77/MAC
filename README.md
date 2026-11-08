# MAC Kundenklärung 1.3.1

Lokale Windows-Desktop-App für die bereitgestellte MAC-XLSM-Vorlage. C# 5 / WinForms / .NET Framework 4.7.2+, ohne externe Client-Laufzeitpakete, lokale Kundendatenbank, WebView oder Telemetrie. Version 1.3 ergänzt einen zentralen HTTPS-Anmeldedienst (Python/WSGI, SQLite für Konten und Sitzungen). Kundeneinträge und ihre verschlüsselte Wiederherstellung bleiben lokal. Nichts wurde gepusht oder veröffentlicht.

**Zuerst EINRICHTUNG_1.3.1.md lesen.** Der Anmeldedienst muss zusätzlich zu GitHub bereitgestellt werden. Die öffentliche Serveradresse `https://mac-anmeldung.onrender.com` ist fest hinterlegt. Zugangsdaten und Konten werden nicht mitgeliefert.

## Projekt

- `auth-server/`: zentrale Passwortprüfung, Rollen, Sitzungen, Rate-Limits, Docker, Sicherungswerkzeug und Tests.
- `render.yaml`: vorbereitetes Hosting in Frankfurt mit dauerhaftem Speicher.
- `src/Auth.cs`, `src/AuthUi.cs`: HTTPS-Client, DPAPI-Anmeldenachweis, Anmeldung und Admin-Kontoverwaltung.
- `src/App.cs`: Fenster, Formular, Vorschau, Filter, Duplikatsprung, Rückgängig, Import/Export und Sitzungsablauf.
- `src/ModernControls.cs`: abgerundete Controls, Zifferneingabe mit sicherer Einfügeprüfung, Auswahl ohne Mausrad-Wertänderungen und gepufferte Darstellung.
- `src/MultiSelect.cs`: modale, durchsuchbare Sparten-Mehrfachauswahl mit Übernehmen/Abbrechen.
- `src/Theme.cs`: Farbpaletten und MAC-Dialogfenster.
- `src/Workbook.cs`: gemeinsamer Validator, eindeutige 8–12-stellige Polizzen, Namensanfänge, OOXML-Import/Export und VBA-Erhalt.
- `src/LocalState.cs`: versioniertes binäres Sitzungsformat ohne BinaryFormatter, DPAPI CurrentUser, atomisches Schreiben, Einstellungen und Instanzsperre.
- `src/Updater.cs`: ausschließlich manuelle GitHub-Abfrage/Download mit Integritätsprüfung.
- `src/Installer.cs`: Online-Setup, Programmprüfung, feste Installation pro Benutzer und Verknüpfungen.
- `src/Version.cs`: zentrale Versionsnummer; `src/SelfTest.cs`: automatisierte, synthetische Tests.
- `assets/`: kundenfreie XLSM-Vorlage, bereitgestelltes MAC-Logo und daraus abgeleitetes Icon.
- `tests/`: Originalvergleich im RAM, Vorlagenprüfung, Framework-Kompatibilität und Windows-Abnahme.
- `build.ps1`: Windows-Build, Tests und zusammengehörige Release-Dateien.

## Windows-Build

.NET Framework 4.7.2 Developer Pack installieren oder `-ReferenceDirectory` auf offizielle Microsoft-net472-Referenzassemblies setzen. Berater brauchen nur .NET Framework 4.7.2 oder neuer, kein Developer Pack.

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\build.ps1
```

Der Bypass gilt nur für diesen Build-Aufruf, nicht dauerhaft. In verwalteten Umgebungen den Build mit der IT abstimmen. Der Build benötigt bei vorhandenen Referenzen kein Internet und verwendet ausschließlich Framework-Referenzen, keine erweiterten Mono-Laufzeitreferenzen. Der frühere `String.TrimStart(Char)`-Fehler bleibt durch char[]-Aufrufe und IL-Prüfung abgesichert.

Ergebnis in `dist`: `MAC-Setup-1.3.1.exe`, `MAC-Programm-1.3.1.zip`, `SHA256SUMS.txt`. Das Setup enthält die Prüfsumme genau dieses Programm-ZIPs. Dateien unterschiedlicher Builds nicht mischen.

## Testen

`MAC-Kundenklaerung.exe --self-test` führt fachliche Tests, Speicherformat- und Einstellungsprüfungen aus. Unter Windows werden zusätzlich DPAPI-Verschlüsselung, Wiederherstellung, Manipulationsabwehr und Entwurfslöschung mit synthetischen Daten in einem temporären Testordner geprüft, der anschließend entfernt wird. Unter Linux wird dieser Windows-spezifische Teil ausdrücklich übersprungen.

`--ui-smoke` verwendet einen Testmodus ohne Zugriff auf das echte lokale Benutzerprofil. Es prüft Oberfläche, Einfügen, Mausradschutz, Duplikate, Namen, Filter, Rückgängig, Wiederherstellung im RAM und modale Auswahl. Ausschließlich in diesem Diagnosemodus entstehen synthetische Screenshots unter `%TEMP%`: `mac120-light.png`, `mac120-dark.png`, `mac120-sparten.png`.

`tests/CheckOriginal.cs` prüft die bereitgestellte Originaldatei ausschließlich im RAM. Es erzeugt keine Kundendatei. Eine Originalpolizze entspricht nicht der neu vorgegebenen 8–12-Ziffern-Regel und wird nicht erfunden oder ersetzt. Die übrigen 61 Datensätze werden im Test einzeln mit explizit ergänzten Leerfeldern exportiert und wieder eingelesen. Tatsächlicher Prüfstand in TESTBERICHT.md, Windows-Abnahme in tests/WINDOWS.md.

## Datenregeln und Speicherung

Exakt zehn Kundenfelder: VN, Polizzennr., Sparte, Antwort Betreuer, Inkassostatus, Kündigung, KFZ Risiko, VM vorhanden?, SS, offene Prämie. Alle Felder verpflichtend; außer bei Polizzennr. gilt die ausdrückliche Ausnahme „Nicht angegeben“. Polizzen bestehen aus 8–12 ASCII-Ziffern, führende Nullen bleiben erhalten. Duplikate werden innerhalb der geöffneten Liste geprüft, nicht über andere Dateien/Rechner hinweg. Importierte ungültige Nummern bleiben zur Korrektur erhalten. Namens-Wortanfänge werden wie gewünscht großgeschrieben, Binnen-Großschreibung bleibt erhalten.

Vorlage: ein Blatt Tabelle1, Daten ab Zeile 5, Freeze A5, Drucktitel Zeilen 1–4, vier Excel-Auswahllisten, keine Zellformeln, VBA vorhanden. Der Export hält VBA bytegleich und verwendet XLSM. Die Prüfspalte in der Vorschau ist kein zusätzliches Kundenfeld.

Einstellungen und Entwurf: `%LOCALAPPDATA%\MAC-Versicherungsagentur-Daten\`, getrennt von den Programmdateien. Kundendaten werden nur nach DPAPI-Verschlüsselung auf den Datenträger geschrieben. Bei Verschlüsselungsfehlern gibt es keinen Klartext-Fallback. Wiederherstellung wird vor dem Start der Autosicherung angeboten; Abbruch lässt den alten Entwurf bestehen. Speichern ca. alle zwei Sekunden und beim normalen Schließen. Nach Excel-Export/Zurücksetzen/Verwerfen Entwurf löschen. Gleichzeitige App-Instanzen werden über eine Dateisperre verhindert. Vollständige Datenfluss- und Betriebssystemgrenzen in ANLEITUNG.md.

DPAPI-Dokumentation: https://learn.microsoft.com/dotnet/api/system.security.cryptography.dataprotectionscope

## GitHub-Veröffentlichung

Den Inhalt von `Quellcode/MAC-App` aus dem Administratorpaket in die Wurzel von `Tunfisch77/MAC` übernehmen, einschließlich `src`, `assets`, `tests`, Buildskript, Dokumentation, `.gitignore` und `.github/workflows/build.yml`. Keine Original-Kundendateien oder lokalen Entwürfe hochladen. `assets/template.xlsm` ist bereinigt.

Unter https://github.com/Tunfisch77/MAC/releases/new den Tag **v1.3.1** erstellen. Die drei zusammengehörigen Dateien aus `Release-Dateien` anhängen und regulär/öffentlich veröffentlichen, nicht als Draft oder Pre-release. Das Programm-ZIP unverändert hochladen. Danach unter Windows testen; Berater erhalten nur die EXE. Der Workflow baut und prüft auf Windows und lädt ein Build-Artefakt hoch, veröffentlicht aber kein Release; nur Leserechte.

Für weitere Versionen `src/Version.cs` erhöhen, Anleitung aktualisieren und neu bauen. Bereits veröffentlichte Pakete nicht ersetzen. GitHub muss für den Installer einen SHA-256-Digest liefern; andernfalls verweigert der Updater die Installation. Private Releases ohne öffentlichen Download werden nicht unterstützt. Vollständige Online-Anleitung in ONLINE_SETUP.md.

## Zentrale Anmeldung entwickeln und testen

`python -m unittest discover -s auth-server/tests -v` benötigt nur Python 3.12. `build.ps1` baut zusätzlich `dist/AuthIntegration.exe`. Mit `python auth-server/tests/client_integration.py dist/AuthIntegration.exe` läuft der echte .NET-Client gegen einen isolierten Loopback-Server. Dieser Test setzt die Serveradresse per Reflection auf HTTP; der Produktionsclient verwendet die fest eingebaute HTTPS-Adresse. Nur synthetische Testkonten, keine echten Zugangsdaten.

`tests/AuthUiSmoke.cs` wird für einen separaten UI-Test zusammen mit den App-Quellen und `/main:AuthUiSmoke` gebaut. Er prüft die Anmeldemaske und die Adminauswahl gegen den Loopback-Testserver. Die normale App benötigt zur Nutzung einen echten, korrekt eingerichteten HTTPS-Anmeldedienst.

Server: Python 3.12 und der in `auth-server/requirements.txt` gepinnte Gunicorn. Kein Framework nötig. Passwortableitung PBKDF2-HMAC-SHA256 mit 600.000 Runden und zufälligen 24-Byte-Salts; zufällige 256-Bit-Anmeldenachweise, nur deren SHA-256-Werte in der Serverdatenbank. SHA-256 wird hier ausschließlich für zufällige Tokens, nicht als Passwortableitung verwendet.

Das Konto ist durch normalisierten Vor- und Nachnamen eindeutig. Adminzugriff wird bei der Anmeldung serverseitig anhand des gemeinsamen Adminpassworts erkannt; kein kundenseitiger Passwortvergleich. Reset/Sperre beendet alle Sitzungen eines Kontos. Admin-Hash-Wechsel entwertet Adminsitzungen. Beratersitzung maximal 8 Stunden ohne Merken, 30 Tage mit Merken; Admin maximal 8 Stunden ohne dauerhafte lokale Anmeldung.

Die Client-Authentifizierung ist keine Manipulationssperre gegen einen Angreifer mit Kontrolle über die lokale EXE/Windows-Umgebung. Es gibt kein serverseitiges Kundenregister oder eine digitale Signatur für Excel-Dateien.

### Proxyprüfung unter Linux

Mit den installierten `auth-server/requirements.txt`: `python auth-server/tests/proxy_integration.py`. Startet Gunicorn auf einem freien lokalen Port mit synthetischem Adminhash und temporärer Datenbank. Prüft HTTP-Abweisung, weitergeleitetes HTTPS und HEAD-Antworten; benötigt keinen produktiven Dienst.
