# MAC Kundenklärung 1.2.0

- Breiten Außenrand entfernt, Kopfbereich kompakter, abgerundete Felder/Buttons und gepufferte Darstellung.
- Polizzennummern strikt 8–12 ASCII-Ziffern; keine Buchstaben, Leerzeichen oder „Nicht angegeben“. Führende Nullen bleiben erhalten. Ungültiges Einfügen wird vollständig abgewiesen; importierte Nummern nicht gekürzt.
- Doppelte Polizzen innerhalb der geöffneten Liste blockiert, gleiche Namen erlaubt. Direkter Sprung zum vorhandenen Eintrag. Ungültige/importierte Duplikate bleiben zur Korrektur sichtbar.
- Sparten in einem modalen Fenster mit Suche, Mehrfachauswahl, Übernehmen und Abbrechen.
- Andere Auswahlfelder ändern sich nicht mehr durch das Mausrad. Geschlossene Auswahlfelder leiten das Scrollen ans Formular weiter.
- Wortanfänge von VN werden großgeschrieben; Binnen-Großschreibung bleibt erhalten. Gilt auch beim Import/Export.
- Hell-/Dunkelmodus, Fenstergröße, Maximierung und Aufteilung lokal gespeichert.
- Verschlüsselte Sitzungswiederherstellung mit Windows DPAPI CurrentUser. Automatische Sicherung ca. alle zwei Sekunden und vor normalem Schließen; Nachfrage beim nächsten Start. Kein Klartext-Fallback. Entwurf nach Export, Zurücksetzen oder Verwerfen entfernen.
- Filter „Noch zu ergänzen“ und Rückgängig für entfernte Einträge innerhalb der laufenden Sitzung.

Datenschutzänderung auf ausdrücklichen Wunsch: Kundendaten liegen nun zusätzlich in einer verschlüsselten lokalen Entwurfsdatei, nicht ausschließlich in RAM/Excel. Es gibt keine Kundenübertragung, Telemetrie oder Kundenlogs. Der Datenordner liegt getrennt vom Installationsordner. Einzelheiten in ANLEITUNG.md.

Die ursprüngliche Datei enthält eine Polizzennummer, die der neuen Eingaberegel nicht entspricht. Sie muss vor einem Gesamtexport bewusst korrigiert werden. Sie wurde weder ersetzt noch erfunden.

Die drei Release-Dateien gemeinsam unter dem öffentlichen, regulären Tag v1.2.0 veröffentlichen. Programm-ZIP unverändert lassen. Berater erhalten nur MAC-Setup-1.2.0.exe. Nichts wurde von hier veröffentlicht.

Build, fachliche Tests, Speicherformat, Einstellungen und Oberfläche unter Mono geprüft. Native Windows-DPAPI, Windows-Installation, Skalierung und echter GitHub-Download müssen unter Windows geprüft werden. Der Installer führt vor der Installation einen Selbsttest inklusive DPAPI auf dem Windows-PC aus. Das ersetzt keine vollständige Windows-Abnahme.
