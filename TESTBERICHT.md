# Testbericht – MAC Kundenklärung 1.3.1

## Prüfstand 8. November 2026

- App und Installer mit C# 5 und offiziellen Microsoft-.NET-Framework-4.7.2-Referenzen kompiliert. Framework-API-Prüfung erfolgreich (7 String-Aufrufe in der App, 1 im Installer).
- App-Selbsttests erfolgreich: Excel-Export/Wiederimport, unveränderte zehn Werte, Makros bytegleich, Fixierung der ersten vier Zeilen, Beraterkopf/Dateiname, Pflichtfelder, Zahlen/Prozente/Euro, führende Nullen, Duplikate und lokale Update-Fixtures.
- Installer-Selbsttests erfolgreich: festes GitHub-Repository, eingebettete SHA-256-Prüfsumme, ZIP-Pfadschutz.
- Zehn Python-Testgruppen erfolgreich: bestehende Anmeldung, automatische Anmeldung, Rollen, falsche/verdrehte Namen, unbekannte/gesperrte Konten, Adminverwaltung ohne Namen, Adminzugriff mit Namen, Passwortwechsel/Reset, Sitzungswiderruf/Ablauf, Hashrotation, bestehende Datenbank, gemeinsame Rate-Limits beider Loginrouten, Eingabe-/Transportprüfung. Wiederverwendung des Adminpassworts als neues Beraterpasswort wird blockiert; bestehende Überschneidung gibt keine zusätzlichen Rechte.
- Tatsächlicher C#-Client gegen isolierten lokalen WSGI-Dienst erfolgreich: Statusprüfung, Anmeldung, Rollen, Fehlermeldungen, Passwortreset, Widerruf, Adminprofilwechsel und Abmelden. Alte lokale Adressdatei kann den festen Server nicht überschreiben. Automatisch erkannter Admin wird auch mit „Angemeldet bleiben“ nicht auf Disk gespeichert. Keine Credentialdateien nach Abschluss.
- Produktions-Gunicorn mit neuer Konfiguration gestartet: Render-Voreinstellung, HTTP abgewiesen, weitergeleitetes HTTPS akzeptiert, GET-Status und HEAD ohne Body. Keine Control-Socket-Berechtigungsfehler. Der laufende Render-Dienst wurde nicht verändert.
- GUI unter Mono/Xvfb erfolgreich: drei Eingabefelder ohne Adresse/Admincheckbox, Passwort anzeigen/verbergen, Pflichtpasswort, Abbrechen, vollständige automatische Adminanmeldung über den Anmeldeknopf, Verwaltung und Profilwahl. Hell-/Dunkelansicht visuell geprüft; keine abgeschnittenen Beschriftungen in der geprüften Größe.
- Bestehender Formular-GUI-Test erfolgreich: Einfügen/Ziffern, Mausradschutz, Duplikate, Filter, Rückgängig, Wiederherstellung im RAM, Spartenpopup, Themenwechsel und Zurücksetzen.
- Bibliotheken unverändert. Keine neue Telemetrie, keine Kundenwerte in Authentifizierungsanfragen. Login enthält nur Namen, Passwort und Merken-Wunsch. Serverrolle entscheidet über lokale Tokenpersistenz.
- Releasepaket enthält nur den vorgesehenen kundenfreien Programminhalt; SHA-256 von ZIP und Setup wird vor Übergabe geprüft.

## Grenzen

Kein nativer Windows-Rechner verfügbar. Installation/Verknüpfungen, Windows-DPAPI und Skalierung 125/150 % sind hier nicht nativ geprüft. Der Installer führt die vorhandenen Selbsttests unter Windows aus; die native Abnahme steht in tests/WINDOWS.md. GUI-Tests liefen unter Mono und ersetzen diese Abnahme nicht. Keine echten Konten, keine Passwörter des Benutzers und kein Login beim produktiven Render-Dienst verwendet. GitHub/Render müssen vom Administrator mit den neuen Dateien aktualisiert werden.
