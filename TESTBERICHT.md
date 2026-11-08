# Testbericht – MAC Kundenklärung 1.3.0

## Durchgeführte Prüfungen

- Windows-App und Online-Installer mit offiziellen Microsoft-.NET-Framework-4.7.2-Referenzen und C# 5 kompiliert. Keine Mono-Laufzeitassemblies als Buildreferenzen.
- Selbsttests von App und Installer unter Mono erfolgreich: Pflichtfelder, Ausnahmen, 8–12 ASCII-Ziffern, führende Nullen, Doppelpolizzen, Namensanfänge, Prozent/Euro, Mehrfachauswahl, Update-Fixtures, URL-/SHA-256-Prüfung und ZIP-Pfadschutz.
- IL-Prüfung gegen den früheren `TrimStart(Char)`-Fehler: App sieben Framework-kompatible char[]-Aufrufe, Installer ein Aufruf; bestanden.
- Beraterexport und Wiederimport geprüft: B3 enthält Beraternamen, zehn Kundenwerte unverändert, VBA bytegleich, Freeze A5 unverändert. Erneuter Export mit anderem Berater ersetzt ausschließlich die Beraterzuordnung im Kopf.
- Serveradresse: HTTP, Benutzerinfo in URL, Pfad, Query, Loopback und abweichender Port abgewiesen; gültige HTTPS-Adresse normalisiert. Keine Redirect-Freigabe oder Abschaltung der Zertifikatsprüfung.
- Sechs automatisierte Python-Testgruppen erfolgreich: gesalzene Passwortableitung und Namensnormalisierung; Namens-/Passwortzuordnung und Rollen; Adminzugriff; Passwortwechsel/Reset/Sperre/Abmeldung; Tokenablauf und Admin-Hash-Rotation; Beständigkeit über Serverneustart; doppelte Namen; Begrenzung von Anmeldeversuchen; ungültige, zu große und unerlaubte Anfragen.
- Tatsächlicher C#-Client gegen isolierten laufenden WSGI-Testserver geprüft: Anmelden, falsches Passwort, Adminliste, Rollenabwehr, Reset/Widerruf, Admin-Profilwahl und Abmelden. Kein lokales Credentialfile ohne „Angemeldet bleiben“. Nur synthetische Konten.
- Produktions-Gunicorn-Konfiguration gestartet: Dienstinitialisierung, SQLite-Datei, Healthcheck, vertrauenswürdige HTTPS-Proxybehandlung und Adminlogin erfolgreich. Keine Passwörter in Prozessausgabe.
- Render-Blueprint gegen das offizielle JSON-Schema validiert. Dauerhafte Disk konfiguriert, Adminhash ohne Standardwert, automatische Deploys deaktiviert.
- GUI-Tests unter Mono/Xvfb in großem Fenster und bei 1100×720: Formular, Mausradschutz, Einfügen, Filter, Duplikate, Rückgängig, Wiederherstellung im RAM, Sparten-Dialog, Hell-/Dunkelmodus, Updates und Zurücksetzen erfolgreich.
- Zusätzlicher GUI-Test: Anmeldemaske, verdecktes Passwortfeld, Abbrechen ohne Anmeldung, laufende Adminliste und Auswahl eines Beraterprofils erfolgreich. Anmeldemaske und Adminverwaltung visuell kontrolliert.
- Bereinigte Vorlage gegen bereitgestelltes Original geprüft: ursprüngliche Überschriften/Hinweise, Stile und VBA unverändert; keine ursprünglichen Kundenkennungen in der Vorlage. Kein Originalkundenbestand wird mitgeliefert.
- Paketprüfung: inneres Programm-ZIP enthält genau fünf freigegebene Programmdateien. Installer ist an dessen SHA-256 gebunden; Release-Prüfsummen stimmen. Quellpaket enthält keine Laufzeitdatenbanken, lokalen Entwürfe oder echten Zugangsdaten.

## Datenflussprüfung

`Auth.cs` sendet ausschließlich feste Login-/Kontoverwaltungsobjekte und Sitzungstokens an die konfigurierte HTTPS-Adresse. Es erhält keine Referenz auf `Book`, Kundenzeilen oder Formulareinträge. `Updater.cs` überträgt nur Updateanfragen an GitHub. `auth-server/server.py` besitzt keine Kunden- oder Datei-Upload-Endpunkte, verwirft unbekannte JSON-Felder und protokolliert keine Requestkörper, Passwörter oder Bearer-Tokens. Gunicorn-Accesslogs sind ausgeschaltet. Bibliotheken: Client ausschließlich .NET Framework; Server Python-Standardbibliothek und Gunicorn, kein Analyse-/Telemetrie-SDK. Hosting-Infrastruktur kann Verbindungsmetadaten erfassen; keine pauschale Aussage über fremde Systeme.

Lokal werden nur Einstellungen, Serveradresse, optional Windows-geschützter Sitzungsnachweis und die ausdrücklich autorisierten verschlüsselten Entwürfe geschrieben. Excelbytes entstehen im RAM und werden direkt am gewählten Ziel geschrieben. Es gibt keine lokale Passwortliste und keine offline gültige Passwortprüfung. Der Server speichert gesalzene PBKDF2-HMAC-SHA256-Hashes und Hashes zufälliger Anmeldenachweise. Der gemeinsame Adminhash kommt ausschließlich aus einer Hosting-Umgebungsvariable.

Die regelmäßige Onlineprüfung begrenzt den Zeitraum nach einer Kontosperre, ist aber keine sofortige Fernsperre. Sie pausiert bei geöffneten modalen Dialogen. Vor dem eigentlichen Excel-Schreiben, nach dem Dateidialog, wird erneut online geprüft. Eine manipulierte lokale EXE, andere Prozesse im selben Windows-Konto oder frei bearbeitbare Excel-Dateien werden dadurch nicht gegen einen lokalen Angreifer geschützt.

## Noch auf Windows / dem echten Hosting zu prüfen

Hier stand keine native Windows-Desktopumgebung und kein Docker-Daemon zur Verfügung. Die EXE wurde kompiliert und unter Mono getestet, jedoch nicht als echter Windows-Installationslauf ausgeführt. Insbesondere Windows-DPAPI für Anmeldenachweise, lokale Profiltrennung auf dem Dateisystem, 125/150-%-DPI, Windows-Dateidialoge, PowerShell-Hashskript und Excel-Makroausführung bleiben im Zielsystem zu prüfen.

Der Installer startet unter Windows die Offline-Selbsttests. Diese enthalten dort zusätzlich DPAPI-Rundlauf und Löschung für den Anmeldenachweis sowie Verschlüsselung/Manipulationsabwehr und Trennung der Beraterentwürfe. Unter Linux wird dieser Windows-spezifische Teil ausdrücklich übersprungen; er gilt hier nicht als bestanden.

Die Client/API-Integration nutzt absichtlich HTTP nur auf Loopback und eine ausschließlich im Test per Reflection gesetzte URL. Der Produktionsdialog akzeptiert nur HTTPS. Der produktive TLS-Zugang, Render-Mountrechte, Neustarts, Sicherung/Wiederherstellung und die öffentliche GitHub-Downloadstrecke sind nach deiner Einrichtung zu testen. Der Docker-Build und der Render-Deploy wurden hier nicht tatsächlich ausgeführt. Das vollständige Abnahmeprogramm steht in `tests/WINDOWS.md`.

Es wurden keine echten Passwörter vergeben, kein Dienst veröffentlicht und keine Änderungen nach GitHub gepusht.
