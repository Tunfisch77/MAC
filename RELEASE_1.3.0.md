# MAC Kundenklärung 1.3.0

Zentrale Online-Anmeldung für Berater und gemeinsamer Adminzugriff. Beratername im Excel-Kopf und automatischer Dateinamenvorschlag. Voraussetzung: eigener eingerichteter HTTPS-Anmeldedienst; Anleitung EINRICHTUNG_1.3.0.md.

- Vorname/Nachname plus Beraterpasswort; Prüfung ausschließlich durch den Server.
- Expliziter Adminmodus: gemeinsames Adminpasswort plus Beratername öffnet dessen aktives Profil. Ohne Namen öffnet sich die Kontoverwaltung.
- Konten anlegen, sperren/freigeben, Passwörter zurücksetzen und Sitzungen beenden.
- Eigenes Passwort ändern; alle alten Anmeldenachweise werden dadurch entwertet.
- Berater können angemeldet bleiben; Windows schützt den widerrufbaren Anmeldenachweis. Kein Passwortspeicher auf dem Client. Adminanmeldung wird nicht dauerhaft gespeichert.
- Separate lokale Entwürfe pro Konto/Server. Alte Entwürfe nur durch ausdrückliche Adminzuordnung übernehmen.
- Onlineprüfung beim Start, im Hauptfenster regelmäßig und beim Export nochmals nach dem Dateidialog. Keine Kundendatenübertragung.
- Beratername in der bisher leeren Zeile 3; zehn Kundenspalten, Makros und fixierte Zeilen 1–4 bleiben erhalten.
- Servercode, Docker-/Render-Konfiguration, lokale Tests, Windows-Build und deutsche Einrichtungsanleitung enthalten.

Vor Veröffentlichung Server einrichten und auf Windows testen. Reguläres öffentliches Release v1.3.0 mit den drei unveränderten Dateien MAC-Setup-1.3.0.exe, MAC-Programm-1.3.0.zip und SHA256SUMS.txt. Der innere Programm-ZIP darf nicht neu gepackt werden. Quellcode und Release-Dateien enthalten keine echten Kennwörter, Konten oder Kundendaten. Nichts wurde automatisch gepusht oder veröffentlicht.
