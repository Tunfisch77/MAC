# MAC Kundenklärung 1.1.0

- Neues MAC-Design mit hellem und dunklem Farbschema, durchgehend lesbaren Eingaben und eigenen Dialogfenstern.
- Alle zehn Felder verpflichtend; „Nicht angegeben“ als bewusste Ausnahme in jedem Feld. Keine automatische Ergänzung und keine erfundene Null bei fehlenden Zahlen.
- Fehlende/ungültige Angaben direkt am Feld. Auch importierte unvollständige Datensätze verhindern den Export.
- Sparten-Dropdown mit Suche, Mehrfachauswahl und einzeln entfernbaren Kürzeln. Die Ausnahme ist exklusiv.
- Durchsuchbare Vorschau mit Vollständigkeitsstatus sowie Prozent-/Euroanzeige. Die Excel-Datei behält exakt die zehn bisherigen Kundenfelder.
- Updateprüfung ohne vorheriges Speichern möglich. Erst vor der Installation wird bei nicht exportierten Eingaben ein erfolgreicher Excel-Export verlangt.
- Makros, Originalwerte und fixierte Excel-Zeilen 1–4 bleiben erhalten. .NET-Framework-Kompatibilitätskorrektur aus 1.0.1 weiterhin enthalten.

Die Berater erhalten nur MAC-Setup-1.1.0.exe. Als Administrator zuerst die drei Dateien aus Release-Dateien an ein öffentliches reguläres GitHub-Release mit dem Tag **v1.1.0** anhängen. Das MAC-Programm-1.1.0.zip unverändert hochladen, nicht entpacken oder neu packen. Vorhandene ältere Release-Dateien nicht ersetzen.

Nichts wurde von hier veröffentlicht. Build, Datenprüfung, Oberflächentests und Paketprüfung wurden lokal unter Mono ausgeführt. Die tatsächliche Installation, Windows-Skalierung und der Download des neuen Releases müssen nach Veröffentlichung auf einem Windows-PC geprüft werden. Details in TESTBERICHT.md und tests/WINDOWS.md.
