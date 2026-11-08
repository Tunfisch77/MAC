# MAC Kundenklärung 1.3.1

## Installation

Windows 10/11 mit .NET Framework 4.7.2 oder neuer. Excel ist zum Erfassen und Exportieren nicht erforderlich. Berater benötigen ausschließlich `MAC-Setup-1.3.1.exe`. Während der Installation ist Internet erforderlich: Das passende Programmpaket muss vorher im öffentlichen GitHub-Release `v1.3.1` liegen. Einzelheiten für den Administrator stehen in ONLINE_SETUP.md.

Programmordner: `%LOCALAPPDATA%\MAC-Versicherungsagentur\`. Die Installation erfolgt pro Windows-Benutzer ohne Administratorrechte. Startmenü- und, wenn erlaubt, Desktop-Verknüpfungen werden erstellt. Der Installer ist unsigniert; bei einer Sperre auf verwalteten PCs die Freigabe durch die IT verwenden.

## Zentrale Anmeldung

Die App verbindet sich automatisch mit `https://mac-anmeldung.onrender.com`. Es gibt kein Adressfeld; eine alte `auth-server.txt` wird nicht mehr ausgewertet. Vorname und Nachname in die richtigen Felder eintragen und dein Beraterpasswort eingeben. Groß-/Kleinschreibung des Anmeldenamens ist unerheblich. Ein Konto muss zuvor von einem Admin angelegt worden sein.

**Anzeigen / Verbergen** macht das eingegebene Passwort vorübergehend sichtbar. Bei einem Fensterwechsel und beim Anmelden wird es wieder verdeckt.

**Angemeldet bleiben** merkt sich einen Windows-geschützten Anmeldenachweis für höchstens 30 Tage. Kein Passwort und keine Passwortliste werden auf dem PC gespeichert. Der Server prüft die Anmeldung beim Start, ungefähr jede Minute im Hauptfenster und vor jedem Excel-Export. Während geöffneter Dialoge pausiert die Hintergrundprüfung. Ohne Internet bzw. bei ungültiger Anmeldung wird die Bearbeitung gesperrt; der lokale Entwurf bleibt erhalten.

Über **Konto / Abmelden** kannst du den Berater wechseln, dich abmelden oder dein eigenes Passwort ändern. Passwortänderung: bisheriges Passwort plus zweimal neues Passwort (14–128 Zeichen); anschließend neu anmelden. Eine Änderung beendet auch die anderen Sitzungen dieses Kontos.

**Adminzugriff:** Das Passwort wird auf dem Server automatisch als Berater- oder Adminpasswort erkannt; es gibt kein zusätzliches Kästchen. Mit Vor- und Nachnamen eines bestehenden aktiven Beraters öffnet das gemeinsame Adminpasswort dessen Profil. Ohne beide Namen öffnet es die zentrale Kontoverwaltung. Dort lassen sich Konten anlegen, Passwörter zurücksetzen, Konten sperren/freigeben und Sitzungen beenden. Adminanmeldungen werden nicht über einen App-Neustart hinweg gespeichert und gelten höchstens 8 Stunden. Wer das gemeinsame Adminpasswort kennt, hat diese Rechte für alle Konten.

Oben in der App steht der angemeldete Berater bzw. ausdrücklich **Adminmodus · Berater: …**. Die App ordnet lokale Entwürfe dem Konto und dem Server zu. Ein Beraterwechsel übernimmt keine Kundeneinträge in das andere Profil. Es gibt keine Synchronisierung von Kundendaten zwischen PCs oder Standorten.

Alte Entwürfe aus Version 1.2 ohne Beraterzuordnung bleiben erhalten. Wenn noch kein Entwurf im neuen Profil existiert, kann ein Admin beim Öffnen dieses Profils den alten Entwurf ausdrücklich zuordnen. Vor dem Update möglichst zuerst als Excel exportieren.

## Erfassen und prüfen

Die App startet ohne mitgelieferte Kundendaten. **Excel öffnen** liest eine vorhandene MAC-XLSM-Datei; die Quelldatei bleibt unverändert und ihre Makros werden nicht ausgeführt.

Alle zehn bisherigen Kundenfelder müssen vor dem Übernehmen bzw. Excel-Export ausgefüllt sein. Für neun Felder ist **Nicht angegeben** als ausdrückliche Ausnahme möglich. Es wird keine fehlende Information automatisch erfunden.

**Polizzennummer:** ausschließlich 8–12 Ziffern von 0 bis 9. Buchstaben, Leerzeichen und Sonderzeichen sind nicht erlaubt; „Nicht angegeben“ ist hier nicht mehr zulässig. Führende Nullen bleiben erhalten. Einfügen eines ungültigen Textes wird vollständig abgewiesen. Importierte Nummern werden nicht gekürzt oder automatisch verändert: Ungültige Nummern bleiben zur Korrektur sichtbar und verhindern den Export.

Jede Polizzennummer darf in der aktuell geöffneten Liste nur einmal vorkommen. Gleiche Namen sind erlaubt. Beim Bearbeiten wird der eigene Datensatz nicht als Duplikat gewertet. Über **Eintrag öffnen** am Polizzenfeld kann bei einem Duplikat zum vorhandenen Eintrag gewechselt werden; unübernommene Änderungen müssen vorher ausdrücklich verworfen werden. Es gibt keinen Abgleich mit anderen Excel-Dateien, anderen Beratern oder einer zentralen Datenbank.

**VN:** Wortanfänge nach Leerraum werden großgeschrieben, z. B. `mustermann max` → `Mustermann Max`. Die übrigen Buchstaben bleiben erhalten, etwa bei `McDonald`. Dies geschieht beim Verlassen des Eingabefeldes, beim Übernehmen und auch beim Import/Export. Die ursprüngliche Quelldatei wird dabei nicht verändert. Diese Namensanpassung ist die ausdrücklich gewünschte Ausnahme vom unveränderten Übernehmen vorhandener Werte.

**Sparte:** Ein eigenes Fenster zeigt die durchsuchbare Mehrfachauswahl. Bereits markierte Sparten bleiben bei einem Suchwechsel ausgewählt. Erst **Übernehmen** bestätigt die Auswahl. **Abbrechen** oder Schließen des Fensters verwirft die Änderungen. Die Kürzel lassen sich anschließend über × einzeln entfernen. „Nicht angegeben“ schließt andere Sparten aus.

**Inkassostatus, Kündigung, VM vorhanden:** Menü öffnen und den gewünschten Wert anklicken. Das Mausrad über dem geschlossenen Auswahlfeld scrollt das Formular und verändert den Wert nicht. Auch das Scrollen innerhalb des geöffneten Menüs übernimmt keinen Wert.

**SS (%)**: `4,29` bedeutet 4,29 %. Bereits vorhandene Excel-Zahlen werden entsprechend ihrer bisherigen Bedeutung angezeigt; ein gespeicherter Wert 4,29 entspricht 429 %. Prozent- und Eurobeträge mit Dezimalkomma ohne Tausendertrennzeichen eingeben. „Nicht angegeben“ wird in diesen beiden Feldern als Text exportiert, niemals als 0.

## Vorschau und Bearbeitung

**Eintrag in Vorschau übernehmen** prüft den Datensatz. In der Vorschau erscheinen „Bereit“, „Ergänzen“ oder „Doppelt“. Diese Prüfspalte gehört nur zur App; die Excel-Datei behält exakt die bisherigen zehn Kundenfelder.

Die Suche durchsucht alle zehn Felder. **Noch zu ergänzen** zeigt unvollständige, ungültige und doppelte Einträge. Doppelklick oder **Auswahl bearbeiten** öffnet die richtige Zeile auch bei aktivem Filter. Weitere Spalten sind horizontal erreichbar; VN und Prüfstatus bleiben sichtbar.

**Entfernen** entfernt nach Bestätigung den ausgewählten Eintrag. **Rückgängig** stellt entfernte Einträge in umgekehrter Reihenfolge wieder her, solange die Sitzung geöffnet ist. Wenn die Polizzennummer inzwischen erneut verwendet wird, wird das Wiederherstellen bis zur Korrektur blockiert. Der Rückgängig-Verlauf wird nicht über Neustarts aufbewahrt und beim Zurücksetzen oder Öffnen einer anderen Datei gelöscht.

**Neuer Eintrag** leert das Formular nach Rückfrage bei unübernommenen Änderungen. **Zurücksetzen** entfernt nach Bestätigung die gesamte Sitzung und die lokale Wiederherstellungskopie. Bereits exportierte Dateien bleiben bestehen.

## Darstellung und Einstellungen

Die Oberfläche nutzt das maximierte Fenster ohne den bisherigen breiten Außenrand. Abgerundete Buttons und Eingabefelder, kompaktere Abstände und gepufferte Darstellung verringern Flackern. Mit **Hellmodus / Dunkelmodus** wechseln die Farben einschließlich eigener App-Dialoge. Farbwahl, Fenstergröße, maximierter Zustand und Aufteilung zwischen Formular/Vorschau werden lokal gespeichert. Windows-Dateidialoge, Fensterrahmen, native Scrollleisten und Sicherheitsmeldungen können weiterhin dem Systemdesign folgen.

## Automatische Wiederherstellung

Diese Version speichert auf ausdrücklichen Wunsch zusätzlich zum Excel-Export einen lokalen, verschlüsselten Sitzungsentwurf. Das ersetzt die frühere Regel „Kundendaten nur im RAM und im Excel-Export“.

Ablage: `%LOCALAPPDATA%\MAC-Versicherungsagentur-Daten\`, getrennt vom Programmordner.

- `settings.json`: ausschließlich Farbe/Fenstergröße/Aufteilung, keine Kundendaten.
- `profiles/<Profilkennung>/session.macdraft`: verschlüsselter Entwurf mit der Arbeitsmappe, übernommenen Einträgen und dem aktuellen, auch unvollständigen Formular.
- Dateien mit `.new`: Zwischenstand des atomaren Schreibens; Kundendaten auch hier ausschließlich verschlüsselt.
- `session.lock`: leere Sperrdatei gegen gleichzeitige App-Instanzen, keine Kundendaten.

Änderungen werden etwa alle zwei Sekunden gesichert; beim normalen Schließen zusätzlich sofort. Bei einem abrupten Prozessabbruch/Stromausfall können die letzten Änderungen seit der letzten erfolgreichen Sicherung fehlen. Die App schreibt zunächst die verschlüsselte neue Datei und ersetzt danach den alten Stand. Sie verwendet **Windows DPAPI / CurrentUser**: Entschlüsseln ist an den aktuellen Windows-Benutzerkontext gebunden. Es gibt keinen Klartext-Ersatz, falls die Verschlüsselung fehlschlägt.

Beim nächsten Start fragt die App nach **Wiederherstellen** oder **Verwerfen**. Abbrechen/Schließen des Wiederherstellungsdialogs schließt die App und lässt den Entwurf bestehen. Eine beschädigte oder nicht entschlüsselbare Datei wird nicht still überschrieben. Bei fehlgeschlagener Sicherung erscheint ein Hinweis; normales Schließen wird verhindert, bis exportiert, erfolgreich gesichert oder ausdrücklich zurückgesetzt wurde.

Nach erfolgreichem Excel-Export, ausdrücklichem Verwerfen oder Zurücksetzen wird die Wiederherstellungskopie gelöscht. Nach weiteren Änderungen entsteht ein neuer Entwurf. Schlägt das Löschen fehl, wird dies angezeigt. Löschen ist normales Dateilöschen, keine garantierte physische Datenvernichtung.

## Excel-Export

**Als Excel speichern** prüft sämtliche Einträge, einschließlich durch Filter ausgeblendeter Zeilen. Ein unvollständiger, ungültiger oder doppelter Eintrag verhindert den gesamten Export. Ein unvollständiger Entwurf darf lokal wiederhergestellt werden; das ist keine Freigabe für den Excel-Export.

Vor dem Schreiben werden Dateiname und Speicherort abgefragt. Es wird `.xlsm` verwendet, damit das vorhandene VBA-Projekt erhalten bleibt. Makros, wichtige Formatierungen und fixierte Zeilen 1–4 bleiben erhalten. Die App führt keine Makros aus. Für die Mehrfachauswahl innerhalb von Excel ist die entsprechende Makrofreigabe in Desktop-Excel nötig.

Maximal 996 Kundenzeilen (Excel-Zeilen 5–1000), ein Blatt `Tabelle1`, genau die zehn vorhandenen Spalten. Dateien mit weiteren Blättern oder Zellformeln werden abgelehnt statt verlustbehaftet umgewandelt. Die untersuchte Vorlage enthält keine Zellformeln. Import maximal 20 MB, entpackt maximal 80 MB. Wiederherstellungsdaten sind zusätzlich größenbegrenzt; bei einer Überschreitung wird die Sicherung nicht als erfolgreich angezeigt.

Exportziele im Programmordner werden abgewiesen. Überschreiben erfolgt erst nach Bestätigung des Speicherdialogs. Stromausfall oder voller Datenträger während des direkten Excel-Schreibens können die gewählte Zieldatei beschädigen; bei wichtigen Beständen einen neuen Dateinamen verwenden.

## Updates und Datenschutz

**Nach Updates suchen** fragt nur nach ausdrücklichem Klick das neueste reguläre Release von `Tunfisch77/MAC` ab. Die Prüfung ist mit ungespeicherten Eingaben möglich. Vor einer tatsächlichen Installation müssen nicht exportierte Eingaben als Excel gespeichert werden. Abbruch oder ungültige Angaben verhindern den Installationsstart. Download nur vom festgelegten Repository bzw. offiziellen Release-Assets und mit SHA-256-Prüfung. Kein Release/kein Netzwerk führt zu einer verständlichen Meldung. Es wurde nichts automatisch veröffentlicht.

Keine lokale Kundendatenbank, Kundenlogs, Telemetrie, WebView, Crash-Uploads oder Cloudspeicherung von Kundeneinträgen. Die Kundenwerte gelangen nicht in Update- oder Loginanfragen. Netzwerkcode befindet sich in Updater.cs und Auth.cs; GitHub bzw. der Anmeldeserver erhalten bei einer Verbindung IP-Adresse und App-User-Agent. Microsoft-.NET-Komponenten für WinForms, XML, ZIP, SHA-256 und DPAPI führen hier keine Kundenübertragung aus. Es wurden keine Analyse-/Telemetriepakete eingebunden. Quellcodeprüfung und Testgrenzen stehen in TESTBERICHT.md.

Die Entwurfsverschlüsselung schützt nicht vor Programmen, die bereits unter demselben Windows-Konto laufen. Systemweite Auslagerungsdateien, Crashdumps, Sicherungsprogramme und fremde Synchronisierungssoftware liegen außerhalb der App. Für ausschließlich lokale Kundendateien einen nicht synchronisierten Exportordner wählen. Verwaltete Zeichenfolgen lassen sich nicht garantiert sofort physisch aus dem RAM entfernen. Exportbytes entstehen im RAM und werden direkt am ausgewählten Ziel geschrieben; Kundendaten werden nicht als unverschlüsselte temporäre Arbeitsdatei abgelegt.

## Deinstallation

App schließen. Programmordner `%LOCALAPPDATA%\MAC-Versicherungsagentur\` und Verknüpfungen entfernen. Vorher bei Bedarf den Entwurf wiederherstellen/exportieren. Zum vollständigen Entfernen lokaler Einstellungen und Entwürfe anschließend auch `%LOCALAPPDATA%\MAC-Versicherungsagentur-Daten\` entfernen. Selbst gewählte Excel-Exportdateien bleiben bestehen.

## Beratername im Export

Vor dem Export wird das Konto online geprüft. In der bisher leeren Zeile 3 steht **Berater: Vorname Nachname**; die bisherigen zehn Kundenspalten und die Fixierung der ersten vier Zeilen bleiben erhalten. Der Dateinamenvorschlag lautet `MAC_Kundenklaerung_Vorname_Nachname_JJJJ-MM-TT.xlsm`. Dateiname und Speicherort bleiben frei wählbar. Auch beim Öffnen einer fremden Excel-Datei bezeichnet der neue Export den **aktuell angemeldeten Berater**. Er ist keine digitale Signatur.

## Online- und lokale Daten ab 1.3

Die früher rein lokale Nutzung wird für die ausdrücklich gewünschte zentrale Anmeldung erweitert. Auf dem Anmeldeserver liegen Beraternamen, gesalzene Passwort-Hashes, widerrufbare Sitzungen und Zähler zur Begrenzung von Anmeldeversuchen. Kundeneinträge werden nicht dorthin übertragen. Lokal liegen die bekannten Einstellungen und ggf. ein Windows-geschützter Anmeldenachweis. Die öffentliche HTTPS-Adresse ist fest im Programm hinterlegt; alte Adressdateien bleiben wirkungslos. Entwürfe liegen verschlüsselt in einem kontospezifischen Unterordner `profiles` des bisherigen Datenordners. Gemeinsame Windows-Konten stellen keine sichere Trennung gegenüber anderen lokalen Programmen dar.

Einrichtung für die Administration: **EINRICHTUNG_1.3.1.md** im vollständigen Projektpaket.
