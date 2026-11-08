# MAC 1.3.0 – Zentrale Anmeldung einrichten

Diese Version besteht aus der Windows-App und einem zentralen Anmeldedienst. Der Dienst ist vorbereitet, aber noch NICHT auf einem öffentlichen Server eingerichtet. Es wurden keine Konten auf GitHub oder Render angelegt und nichts veröffentlicht. Erst nach den folgenden Schritten funktioniert die Anmeldung im Betrieb.

## Was kommt wohin?

| Bestandteil | Ziel |
|---|---|
| Inhalt von `Quellcode/MAC-App/` | In das Hauptverzeichnis des GitHub-Repositorys `Tunfisch77/MAC` |
| Drei Dateien aus `Release-Dateien/` | Als Anhänge an das öffentliche GitHub-Release `v1.3.0` |
| `render.yaml` und `auth-server/` | Werden durch Render aus dem GitHub-Repository gelesen und als Server gestartet |
| Adminpasswort-Hash | Nur als geschützte Umgebungsvariable `MAC_ADMIN_HASH` bei Render |
| Kontendatenbank | Auf dem dauerhaften Serverlaufwerk `/data`, niemals auf GitHub |
| Kundendaten | Auf dem jeweiligen Berater-PC und in der selbst ausgewählten Excel-Datei |

**Das äußere Paket `MAC_GitHub_Release_1.3.0.zip` zuerst entpacken. Die darin enthaltene Datei `MAC-Programm-1.3.0.zip` NICHT entpacken, umbenennen oder neu packen.**

## 1. Vor dem Update

Offene Kundeneinträge in Version 1.2 möglichst zuerst als Excel exportieren. Die neuen Profile haben eigene lokale Entwürfe. Ein alter Entwurf ohne Beraterzuordnung bleibt erhalten; ein Admin kann ihn nach der Anmeldung auf diesem PC ausdrücklich einem Berater zuordnen. Er wird nicht ungefragt dem nächsten Benutzer zugeschlagen.

Richte zuerst den Server ein und teste die neue Version auf einem PC. Veröffentliche das neue Release erst, wenn die Anmeldung funktioniert. Die alte App hat keine zentrale Anmeldepflicht und wird durch einen neuen Server nicht nachträglich gesperrt; ersetze sie auf allen eingesetzten PCs.

## 2. Quellcode auf GitHub bereitstellen

1. Öffne https://github.com/Tunfisch77/MAC.
2. Lade den **Inhalt** von `Quellcode/MAC-App/` in das Hauptverzeichnis des Repositorys. Bestehende Dateien gleichen Namens mit dieser Version ersetzen. Die enthaltenen Dateien unter `.github/workflows/` ebenfalls übernehmen.
3. Auf der obersten Ebene müssen unter anderem `render.yaml`, `auth-server/`, `src/`, `assets/` und `build.ps1` stehen. Nicht noch einen zusätzlichen Ordner `MAC-App` dazwischen anlegen.
4. Lade keine eigenen Excel-Dateien, keine Datenbank, keine Entwürfe und keine Passwörter oder Passwort-Hashes hoch. Die mitgelieferte `assets/template.xlsm` ist von Kundendaten bereinigt und darf als Vorlage mitgeliefert werden.

Das Repository darf für die bestehende Updatefunktion öffentlich bleiben. Der Programmcode enthält weder ein Standardpasswort noch ein echtes Adminpasswort.

## 3. Gemeinsames Adminpasswort festlegen

Wähle eine lange, nur den berechtigten Admins bekannte Passphrase mit **20–128 Zeichen**. Das ist der Generalschlüssel für alle Beraterkonten. Gib diese Passphrase nicht an Berater und nicht in diesen Chat weiter.

Unter Windows:

1. Öffne im entpackten Quellcodeordner `MAC-App` ein PowerShell-Fenster.
2. Starte das mitgelieferte Skript:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\Admin-Passwort-Hash.ps1
```

Der Aufruf gilt nur für dieses Skript; keine dauerhafte Änderung der PowerShell-Richtlinie. Auf verwalteten Firmen-PCs eine eventuelle Sperre durch die IT freigeben lassen.

3. Gib das Passwort zweimal ein. Die Eingabe ist verdeckt.
4. Kopiere die erzeugte Zeile, die mit `pbkdf2_sha256$600000$` beginnt. Das ist der **Hash** für Render. Auch diesen nicht auf GitHub hochladen.

Alternative mit Python 3.12:

```text
cd auth-server
python manage.py hash-admin
```

Das Skript speichert das Passwort nicht in einer Datei. Es benötigt kein Internet. Ein Admin muss später beim Anmelden die gewählte Passphrase eingeben, NICHT den Hash.

## 4. Anmeldeserver bei Render starten

Ein Render-Konto und ein kostenpflichtiger Web-Service mit dauerhaftem Laufwerk sind erforderlich. **Keinen kostenlosen Dienst ohne Persistent Disk verwenden:** Sonst würden die Konten bei einem Neustart/Deploy verloren gehen. Prüfe die angezeigten laufenden Kosten vor dem Anlegen; im Paket ist kein Hostingvertrag enthalten.

1. Öffne https://dashboard.render.com und verbinde das GitHub-Konto mit Zugriff auf `Tunfisch77/MAC`.
2. Wähle **New → Blueprint** und das Repository `Tunfisch77/MAC`.
3. Verwende `render.yaml` im Hauptverzeichnis.
4. Das vorbereitete Blueprint erstellt einen Docker-Web-Service `mac-anmeldung`, Region **Frankfurt**, Tarif **Starter**, mit **1 GB Persistent Disk** unter `/data`.
5. Wenn Render nach **MAC_ADMIN_HASH** fragt, füge den kompletten erzeugten Hash ein, ohne zusätzliche Anführungszeichen. Dies gehört in den geheimen Umgebungswert des Dienstes, nicht in `render.yaml`.
6. Prüfe den Plan und lege den Dienst an. Automatische Deploys sind im Blueprint ausgeschaltet; Änderungen am Server werden später bewusst über **Manual Deploy** veröffentlicht.
7. Warte auf **Live**. Kopiere die angezeigte HTTPS-Adresse, zum Beispiel `https://mac-anmeldung-abc.onrender.com`. Das Beispiel ist KEIN bereits existierender Server für deine Firma.
8. Öffne diese Adresse mit `/health` dahinter. Als Antwort muss sinngemäß `{"service":"mac-auth","api":1}` erscheinen.

Falls der Blueprint nicht benutzt wird, beim manuellen Docker-Web-Service dieselben Werte setzen: Dockerfile `./auth-server/Dockerfile`, Build-Kontext `./auth-server`, Health Check `/health`, Disk `/data`, `MAC_DATABASE=/data/mac-auth.sqlite3`, `MAC_ADMIN_HASH=<dein Hash>`, `FORWARDED_ALLOW_IPS=*`. Letzteres gilt nur hinter Render als einzigem öffentlichen Zugang. Auf einem eigenen Server den Port nicht direkt ins Internet stellen; dort sind HTTPS-Reverse-Proxy und eng begrenzte vertrauenswürdige Proxy-Adressen erforderlich.

Der Container initialisiert die Zugriffsrechte des Datenlaufwerks und betreibt den eigentlichen Dienst anschließend als Benutzer 10001 ohne Rootrechte. SQLite ist für diese kleine, einzelne Serverinstanz vorgesehen; keine zweite Instanz mit separater Kontendatenbank hinzufügen.

## 5. Vor Veröffentlichung lokal testen und Berater anlegen

Für den ersten Test darfst du als Administrator eine **Kopie** des inneren Programmpakets in einen lokalen Testordner entpacken und `MAC-Kundenklaerung.exe` starten. Das Original-ZIP für GitHub unverändert lassen. Dafür ist noch kein neues GitHub-Release nötig.

1. Im Anmeldefenster die HTTPS-Adresse aus Render eintragen, **ohne `/health`** und ohne einen sonstigen Pfad.
2. **Adminpasswort verwenden** markieren.
3. Vorname und Nachname zunächst leer lassen.
4. Das gemeinsame Adminpasswort eingeben und anmelden.
5. In der Kontoverwaltung **Anlegen** wählen. Vorname, Nachname und erstes Beraterpasswort eingeben und bestätigen. Beraterpasswörter: **14–128 Zeichen**.
6. Passwort dem betreffenden Berater über einen geeigneten privaten Weg geben. Der Berater kann es in der App über **Konto / Abmelden → Passwort ändern** ändern.
7. Zum Test einen Berater auswählen und **Als Berater öffnen** anklicken. Oben muss **Adminmodus · Berater: …** stehen.
8. Abmelden und mit dem normalen Beraterpasswort anmelden. Ein falsches Passwort muss abgewiesen werden.
9. Mit ausschließlich künstlichen Testdaten einen Excel-Export durchführen. Beratername in Zeile 3 und vorgeschlagenen Dateinamen prüfen. Erst danach echte Kundendaten verwenden.

**Direkter Adminzugriff wie gewünscht:** Vorname und Nachname des bestehenden Beraters eingeben, `Adminpasswort verwenden` markieren und das gemeinsame Adminpasswort eingeben. Der Server prüft den Adminzugriff; das Beraterpasswort bleibt unverändert. Gesperrte Konten sind auch auf diesem Weg nicht zugänglich, bis ein Admin sie wieder freigibt.

Namen gehören jeweils in das richtige Feld: Vorname `Tuncay`, Nachname `Guliyev`. Groß-/Kleinschreibung und zusätzliche Leerzeichen verhindern die Anmeldung nicht. Zwei Berater dürfen nicht denselben vollständigen Anmeldenamen haben. Bei tatsächlicher Namensgleichheit einen vereinbarten Namenszusatz verwenden; dieser erscheint auch im Export.

## 6. GitHub-Release veröffentlichen

Auf GitHub unter **Releases → Draft a new release**:

- Tag **`v1.3.0`** erstellen/auswählen.
- Titel zum Beispiel **MAC Kundenklärung 1.3.0**.
- Aus `Release-Dateien/` genau diese drei unveränderten Dateien anhängen:
  - `MAC-Setup-1.3.0.exe`
  - `MAC-Programm-1.3.0.zip`
  - `SHA256SUMS.txt`
- Als reguläres, öffentliches Release veröffentlichen, nicht als Draft oder Pre-release; als neuestes Release verwenden.

Die EXE enthält die erwartete SHA-256-Prüfsumme des Programmpakets. Falls das Paket verändert wird, muss auch der Installer neu gebaut werden. Niemals einen alten Installer mit einem neu gepackten Programmpaket mischen.

## 7. Was bekommen die Berater?

Nur **`MAC-Setup-1.3.0.exe`** sowie diese Informationen:

- die HTTPS-Adresse des Anmeldeservers;
- ihr registrierter Vorname und Nachname;
- ihr erstes eigenes Passwort.

Die EXE lädt das passende Programmpaket von GitHub. Auf dem PC muss .NET Framework 4.7.2 oder neuer vorhanden sein. Die App merkt sich die Serveradresse. **Angemeldet bleiben** speichert für Berater einen Windows-geschützten Anmeldenachweis für höchstens 30 Tage. Die Gültigkeit wird auch beim nächsten Start online überprüft. Das Passwort selbst wird nicht lokal gespeichert. Adminanmeldungen werden nach einem App-Neustart nicht wiederaufgenommen; ihr serverseitiger Anmeldenachweis gilt höchstens 8 Stunden.

## 8. Betrieb, Sperren und Passwortwechsel

- **Berater sperren:** Ohne Namen als Admin anmelden → Berater markieren → Sperren/Freigeben. Alle bisherigen Anmeldenachweise werden ungültig.
- **Passwort vergessen:** Admin → Berater markieren → Passwort setzen. Das alte Passwort ist nicht auslesbar; alte Sitzungen werden beendet.
- **Alle PCs eines Beraters abmelden:** Admin → Berater markieren → Sitzungen beenden.
- **Adminpasswort wechseln:** Neuen Hash mit dem Skript erzeugen, bei Render `MAC_ADMIN_HASH` ersetzen und den Dienst neu deployen. Alle bisherigen Adminsitzungen werden ungültig; normale Beraterpasswörter bleiben bestehen.
- **Adminpasswort vergessen:** Ein berechtigter Verwalter des Render-Kontos kann auf demselben Weg einen neuen Hash hinterlegen. Deshalb Render/GitHub-Konten mit eigenen starken Passwörtern und Mehrfaktor-Anmeldung schützen.
- **Wenn ein Admin sein gerade geöffnetes Beraterkonto sperrt oder dessen Passwort zurücksetzt**, wird auch diese auf das Profil bezogene Adminsitzung beendet. Für weitere Verwaltung ohne Beraternamen erneut als Admin anmelden.
- Die App fragt im Hauptfenster ungefähr jede Minute nach der Sitzungsberechtigung. Während modaler Dialoge wird diese Hintergrundprüfung ausgesetzt. Vor jedem Excel-Export erfolgt eine neue Onlineprüfung. Es gibt keine sofortige Fernlöschung lokaler Excel-Dateien oder Entwürfe.
- Bei Ausfall des Servers oder Internets ist keine neue Anmeldung und kein Excel-Export möglich. Der lokale Entwurf wird geschützt behalten; nach Wiederverbindung erneut anmelden.
- Bei fehlgeschlagener serverseitiger Abmeldung, z. B. Netzausfall, wird der lokale Anmeldenachweis trotzdem entfernt. Ein bereits kopierter Nachweis bleibt bis zur zentralen Sperrung oder seinem Ablauf gültig.

## 9. Kontendaten sichern

Die dauerhafte Datenbank `/data/mac-auth.sqlite3` enthält ausschließlich Beraterkonten, Passwort-Hashes, Sitzungen und Zähler zur Begrenzung von Anmeldeversuchen. Sie ist vertraulich. Niemals als GitHub-Anhang veröffentlichen.

In einer Server-Shell kann mit `python manage.py backup --destination /data/mac-konten-backup.sqlite3` eine konsistente SQLite-Sicherung erzeugt werden. Eine vorhandene Zieldatei wird nicht überschrieben. Die Sicherung anschließend in einen zugriffsgeschützten Firmen-Backup-Speicher übernehmen. Eine Kopie auf demselben Laufwerk schützt nicht gegen den Verlust dieses Laufwerks. Für eine Wiederherstellung den Dienst stoppen, die konsistente Sicherung als Datenbank einsetzen und anschließend Sitzungen widerrufen bzw. Adminpasswort wechseln. Den Wiederherstellungsweg vor Produktivbetrieb mit Testkonten prüfen.

## Grenzen und Datenschutz

Ein gemeinsames Adminpasswort identifiziert keinen einzelnen Admin. Wer es kennt, kann alle Konten verwalten und im Namen jedes aktiven Beraters arbeiten. Das ist die ausdrücklich gewünschte Funktion. Der Name im Excel-Dokument ist eine Zuordnung, keine manipulationssichere Unterschrift.

Die App führt keine Kundendatenbank und überträgt keine Kundeneinträge an den Anmeldedienst. Die lokale, verschlüsselte Wiederherstellung bleibt nach Berater und Server getrennt. Sie ist nicht standortübergreifend synchronisiert. Windows-DPAPI schützt Daten gegenüber anderen Windows-Benutzerkonten; Prozesse im selben Windows-Konto bzw. lokale Administratoren sind dadurch nicht zuverlässig ausgesperrt. Gemeinsame Arbeitsplätze möglichst mit getrennten Windows-Konten betreiben.

Für die neue zentrale Anmeldung werden Beraternamen und Kontoinformationen auf dem Server gespeichert; Login-Anfragen übertragen Namen, Passwörter und Sitzungsnachweise per HTTPS. App/API protokollieren keine Passwörter oder Anfrageinhalte und enthalten keine Analyse- oder Telemetriefunktion. Hostinganbieter können Verbindungsmetadaten in ihrer Infrastruktur verarbeiten. Firmenintern muss dieses Hosting freigegeben werden.

## Quellen zur Einrichtung

- Render Blueprint: https://render.com/docs/blueprint-spec
- Dauerhafte Laufwerke: https://render.com/docs/disks
- Docker-Dienste: https://render.com/docs/docker
- OWASP Passwortspeicherung: https://cheatsheetseries.owasp.org/cheatsheets/Password_Storage_Cheat_Sheet.html

Die Anwendung und der Dienst wurden lokal getestet. Ein echter Windows-Installationslauf, die Windows-spezifische Tokenverschlüsselung und die Bereitstellung in deinem Render-Konto sind erst auf dem Zielsystem vollständig prüfbar. Details im TESTBERICHT.md.
