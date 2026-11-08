# Testbericht – MAC Kundenklärung 1.3.2

## Durchgeführte Prüfungen

- App und Installer mit offiziellen Microsoft-.NET-Framework-4.7.2-Referenzen und C# 5 kompiliert. IL-Prüfung: sieben unterstützte String-Aufrufe in der App, einer im Installer.
- App-Selbsttest bestanden: Excel-Export/Wiederimport, zehn Werte, VBA-Erhalt, Freeze A5, Beraterkopf, Pflichtfelder, Polizzen/Nullen/Duplikate, Zahlen/Prozent/Euro und Update-Fixtures.
- Zeitplan über kontrollierte Uhrzeiten geprüft, ohne reale Wartezeit von 24 Stunden: erste Abfrage nach 30 Sekunden, nächste nach einer Stunde; gleiche Version 24 Stunden unterdrückt; neue Version sofort berechtigt; ungültige Zeitangaben blockieren Updates nicht dauerhaft. Gespeicherte Zurückstellung über erneutes Einlesen der Einstellungen bestätigt.
- Echter Formular-GUI-Test unter Mono/Xvfb: automatische Netzwerkfehler ohne Dialog; keine Eingabesperre während laufender Abfrage; keine parallelen Abfragen; Hinweis hinter Dateidialog zurückgestellt; „Später“ erhält den Entwurf; manuelle Suche umgeht die Zurückstellung; Abbruch der Exportvorbereitung verhindert Download/Installation; Schließen während laufender Abfrage ohne späten Dialog. Ausschließlich synthetische Daten und Release-Fixtures, kein echter Download/Installationsstart.
- Update-Popup visuell geprüft: vollständige Beschriftungen, MAC-Farben, „Jetzt aktualisieren“/„Später“.
- Installer-GUI-Abschlusslogik tatsächlich ausgeführt: erfolgreicher Abschluss schließt das Fenster trotz vorherigem busy-Zustand; erfolgloser/unvollständiger Abschluss bleibt sichtbar und ist manuell schließbar.
- Installer-Selbsttest bestanden: festes Repository, eingebettete SHA-256-Prüfsumme, Paket-Pfadschutz. Exaktes neues Fünf-Dateien-Programmpaket vom Installer geprüft.
- Vorhandener Formular-GUI-Test bestanden: Mausradschutz, strenges Einfügen, Namensformat, Duplikate, Filter, Rückgängig, Wiederherstellung im RAM, Popup-Abbruch, Hell-/Dunkelmodus und Zurücksetzen.
- Tatsächlicher C#-Anmeldeclient gegen isolierten lokalen WSGI-Server geprüft: Anmeldung, Rollen, Reset/Widerruf, Profilwahl, Logout. Kein gespeichertes Admin-Ticket. Auth-Client, Server und Vorlagendateien gegenüber 1.3.1 unverändert.
- Quellcodeprüfung: Updateanfrage enthält keine Kundeneinträge, Namen oder Zugangsdaten. Neu persistent sind ausschließlich Updateversionsnummer und Hinweiszeitpunkt in den vorhandenen Einstellungen. Keine neuen Bibliotheken, keine Telemetrie, kein Windows-Dienst oder Autostart.

## Grenzen

Prüfungen unter Linux mit Mono/Xvfb; kein nativer Windows-Rechner verfügbar. Die vollständige Installation samt Windows-Verknüpfungen und Process.WaitForInputIdle sowie DPAPI sind hier nicht nativ geprüft. Der Setup-Selbsttest läuft auf Windows vor der Dateikopie; native Abnahme in tests/WINDOWS.md. GitHub-Release und Render wurden nicht verändert. Der Server benötigt für diese Version kein Update.
