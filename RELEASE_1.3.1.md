# Version 1.3.1

- Feste Anmeldeadresse, keine manuelle Eingabe und keine Auswertung alter Adressdateien.
- Automatische serverseitige Erkennung des Adminpassworts über `/v2/login`; `/v1/login` bleibt für alte Apps erhalten.
- Anmeldemaske mit drei Feldern, Anzeigen/Verbergen, Prüfung leerer/unvollständiger Angaben und getrennten Meldungen für Netz-, Server- und Zugangsdatenfehler.
- Adminsitzungen werden anhand der Serverrolle niemals dauerhaft gespeichert; unbekannte und gesperrte Profile bleiben blockiert.
- Prüfung alter Server vor Übermittlung von Zugangsdaten; bestehende Konten und Dateiformate unverändert.
- Gunicorn-Control-Socket deaktiviert, Render-Proxy-Voreinstellung und korrekte HEAD-Antworten.
- Kein Upload von Kundenwerten; keine neuen Telemetriekomponenten.

Server vor App aktualisieren. Einrichtung siehe EINRICHTUNG_1.3.1.md.
