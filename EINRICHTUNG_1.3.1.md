# MAC 1.3.1 – bestehende Installation aktualisieren

1. Vor dem App-Update offene Kundeneingaben als Excel exportieren und App schließen.
2. Den gesamten Inhalt von `Quellcode/MAC-App/` in die Wurzel des Repositorys `Tunfisch77/MAC` übernehmen. Ordnerstruktur beibehalten, insbesondere `auth-server`, `src`, `assets`, `tests` und `.github`. Keine Kundendateien, Entwürfe, Passwörter oder Hashes hochladen.
3. Beim bestehenden Render-Dienst die Variablen prüfen: `FORWARDED_ALLOW_IPS` = `*`, `MAC_DATABASE` = `/data/mac-auth.sqlite3`, `MAC_ADMIN_HASH` = der bereits erzeugte vollständige Hash. Nicht durch das Klartextpasswort ersetzen. Dauerhafter Datenträger muss weiterhin unter `/data` eingebunden sein. Bestehenden Dienst/Disk/Konten nicht löschen.
4. Manual Deploy → Deploy latest commit. Der Dienst muss weiterhin unter `https://mac-anmeldung.onrender.com` erreichbar sein. Nach Live unter dieser Adresse prüfen: JSON mit `service: mac-auth`, `api: 1`, `auto_login: true`. `/health` allein testet die HTTPS-Erkennung nicht; die Hauptadresse tut es.
5. Öffentliches reguläres GitHub Release `v1.3.1` erstellen (kein Draft/Pre-release). Genau drei zusammengehörige Dateien aus `Release-Dateien` anhängen: `MAC-Setup-1.3.1.exe`, `MAC-Programm-1.3.1.zip`, `SHA256SUMS.txt`. Programm-ZIP unverändert hochladen, nicht entpackte Einzeldateien. Kein Passwort oder Hash gehört ins Release.
6. Erst danach `MAC-Setup-1.3.1.exe` ausführen. Die neue EXE lädt ausschließlich das passende veröffentlichte Programmpaket. Alternativ bietet die bestehende App nach Veröffentlichung über „Nach Updates suchen“ dieses Update an.
7. Anmeldung: Vorname + Nachname + Beraterpasswort. Admin: Namen eines bestehenden aktiven Beraters + Adminpasswort oder beide Namen leer + Adminpasswort für Kontoverwaltung. Kein Kästchen, keine Adresseingabe. „Anzeigen“/„Verbergen“ schaltet die Passwortdarstellung um; bei Fensterwechsel wird es wieder verdeckt.

## Reihenfolge und Erhalt

Server zuerst, dann App. Der neue Server unterstützt weiterhin die alte 1.3.0-Anmeldung. Bestehende Konten, Hashes und Sitzungen bleiben auf demselben Datenträger erhalten; keine Datenbankmigration nötig. Die neue App verweigert einen alten Server mit verständlichem Updatehinweis. Alte gespeicherte Adressen werden ignoriert; ein gespeicherter Anmeldenachweis wird nur beim festgelegten Server verwendet.

Adminanmeldungen werden auch bei gesetztem „Angemeldet bleiben“ niemals lokal gemerkt und gelten maximal 8 Stunden. Berateranmeldungen gelten ohne Merken 8 Stunden, mit Merken höchstens 30 Tage. Der Server prüft die Rolle; die App vertraut keiner lokal gewählten Adminoption. Ein gesperrtes oder nicht vorhandenes Beraterkonto lässt sich auch mit Adminpasswort nicht als Berater öffnen. Die Verwaltung bleibt über leere Namen erreichbar.

Neue Beraterpasswörter dürfen nicht mit dem Adminpasswort übereinstimmen. Bei einer bereits bestehenden Überschneidung hat eine passende Berateranmeldung ausschließlich Beraterrechte. In diesem Fall Adminverwaltung mit leeren Namen öffnen und getrennte Passwörter setzen.

## Neue Einrichtung

Falls noch kein Admin-Hash vorhanden ist, `Admin-Passwort-Hash.ps1` lokal starten und eine eigene Adminpassphrase (20–128 Zeichen) festlegen. Nur den vollständigen erzeugten Hash als Render-Secret `MAC_ADMIN_HASH` setzen. Das Klartextpasswort wird zum Anmelden verwendet. Keine Zugangsdaten an den Entwickler oder in den Chat schicken. Für eine neue Render-Installation kann `render.yaml` verwendet werden; der dauerhaft eingebundene Datenträger ist erforderlich und kann Kosten verursachen. Die feste Adresse muss zur tatsächlichen Render-Adresse passen; bei anderem Domainnamen `AuthClient.DefaultServer` im Quellcode vor dem Build ändern.

## Serverkorrekturen

Gunicorn-Control-Socket deaktiviert, damit nach dem Wechsel auf den Dienstbenutzer kein Schreibzugriff auf `/root/.gunicorn` nötig ist. `FORWARDED_ALLOW_IPS` wird ausgewertet; falls nicht gesetzt, verwendet nur eine erkannte Render-Umgebung (`RENDER=true`) den Proxy-Modus `*`, sonst Loopback. `*` ist nur für den abgeschirmten Betrieb hinter dem vertrauenswürdigen Hosting-Proxy vorgesehen. Die HTTPS-Prüfung bleibt aktiv. HEAD-Antworten enthalten keinen Body. Die öffentliche Hauptadresse liefert eine kleine Statusantwort ohne Konten oder Geheimnisse.

Es wurde nichts automatisch hochgeladen, veröffentlicht oder am laufenden Render-Dienst geändert.
