# Windows-Abnahme 1.2.0

Nur synthetische Kundendaten verwenden. Zuerst öffentliches reguläres Release v1.2.0 mit den drei zusammengehörigen Dateien bereitstellen. Keine alten Release-Dateien austauschen.

1. Setup unter Windows 10/11 mit .NET Framework 4.7.2+ starten. Selbsttest einschließlich Windows-DPAPI muss bestehen. Installation ohne Administratorrechte, Verknüpfungen, Start und Reparatur prüfen. Zweiter App-Start wird mit verständlichem Hinweis abgefangen.
2. Alle zehn Pflichtfelder testen. Polizzen: 7/8/12/13 Ziffern, Buchstaben, Leerzeichen, Bindestrich, Zwischenablage mit falschen Zeichen/Überlänge. Nur 8–12 ASCII-Ziffern zulässig; führende Nullen erhalten. Bei Polizze keine Ausnahme. Importierte ungültige Nummern bleiben sichtbar und werden nicht gekürzt.
3. Zwei Einträge mit gleicher Polizze: Übernehmen/Export blockiert. Gleiche Namen mit unterschiedlichen Polizzen zulässig. Eigene Nummer unverändert bearbeiten möglich. „Eintrag öffnen“ springt zum richtigen Duplikat; unübernommene Änderungen werden nicht ohne Rückfrage verworfen.
4. Namen nach Eingabe, Import und Export: Wortanfänge groß, interne Schreibweise z. B. McDonald erhalten. Quelldatei unverändert.
5. Inkassostatus, Kündigung und VM: Feld fokussieren und Mausrad bewegen; Inhalt bleibt. Formular scrollt. Menü öffnen, darin scrollen: keine Übernahme ohne bewusste Auswahl. Sparten-Popup: mindestens drei auswählen, Suche ändern, Auswahl bleibt; Abbrechen verändert nichts, Übernehmen bestätigt. „Nicht angegeben“ exklusiv.
6. Mehrere Einträge: Suche, gefiltertes Bearbeiten, Filter „Noch zu ergänzen“. Entfernen und mehrmals Rückgängig testen. Rückgängig blockieren, wenn entfernte Polizze inzwischen wieder verwendet wird.
7. Hell-/Dunkelmodus, Fenstergröße, Maximierung und Aufteilung ändern, schließen, neu starten: Einstellungen erhalten. 100/150 % Skalierung, maximiertes Fenster ohne breiten Außenrand und Mindestgröße 1100 × 720 prüfen. Suchfeld nicht abgeschnitten; letztes Formularfeld erreichbar.
8. Einen vollständigen und einen unvollständigen Entwurf anlegen. Mindestens zwei Sekunden warten; App ohne Excel-Export schließen. Neustart fragt nach Wiederherstellen. Beide Zustände und Bearbeitungsauswahl erhalten. Nur unvollständige Formulareingabe ohne bereits übernommene Zeile ebenfalls testen.
9. App im Task-Manager nach mindestens zwei Sekunden beenden. Neustart/Restaurierung prüfen. Einträge in settings.json dürfen nicht vorkommen. session.macdraft darf keinen Klartext enthalten. Keine Entschlüsselung unter anderem Windows-Konto erwarten. Fehlende letzte Sekunden bei hartem Abbruch sind dokumentiert.
10. Wiederherstellungsdialog über X/Escape abbrechen: Entwurf bleibt erhalten, App schließt. „Verwerfen“ löscht ausdrücklich. Beschädigten Testentwurf ablehnen und nicht überschreiben. Schreibrechte des Test-Datenordners verweigern: sichtbare Fehlermeldung, kein Klartext-Fallback, normales Schließen bei ungesicherten Eingaben blockiert.
11. Erfolgreicher Excel-Export entfernt Entwurfsdatei und .new. Abbruch/Validierungsfehler entfernt sie nicht. Neue Änderung erzeugt wieder eine Sicherung. Zurücksetzen löscht Entwurf und RAM-Zustand. Beim Öffnen anderer Datei aktuelle Daten nur nach bewusster Rückfrage verwerfen.
12. Excel-Speicherdialog: Name/Ort, Abbruch, Überschreiben, Schreibfehler und Programmordnersperre. XLSM in Excel prüfen: zehn Felder, Polizzen/Nullen, Prozent/Euro, explizite Ausnahmen, Freeze A5 und VBA. Makroausführung separat mit freigegebenen Makros prüfen.
13. Updateprüfung mit ungespeicherten Eingaben möglich. Offline/kein Release verständlich. Vor tatsächlicher Installation Export verlangen; Abbruch und ungültige Angaben dürfen weder Installation noch Datenverlust verursachen. Setup darf den separaten Datenordner nicht überschreiben.
14. Optional mit Process Monitor: keine Kundenlogs, unverschlüsselten Arbeitskopien oder unerwarteten Netzwerkanfragen. Kundendaten nur im ausgewählten Excel-Ziel und DPAPI-verschlüsseltem Profilentwurf; Einstellungen ohne Kundendaten.

## Zusätzliche Abnahme 1.3.1 – ausschließlich synthetische Testdaten

1. HTTPS-Anmeldedienst mit persistentem Speicher einrichten. Erst `/health`, dann Login prüfen. Fehlender/falscher Server darf keinen Zugang ermöglichen. HTTP und Weiterleitungen dürfen nicht akzeptiert werden.
2. Leere Namen + Adminpasswort: Verwaltung öffnet sich. Zwei Testberater anlegen. Gleicher Name in anderer Großschreibung muss abgewiesen werden.
3. Richtiges Beraterpasswort/falscher Name und richtiges Profil/falsches Passwort müssen scheitern. Normale Anmeldung darf keine Kontoverwaltung erlauben.
4. Name + gemeinsames Adminpasswort: richtige Profilzuordnung und sichtbarer Adminmodus. Gesperrtes Konto darf nicht geöffnet werden.
5. Eigenes Passwort ändern, erneut anmelden. Auf zweitem PC muss die alte Sitzung nach Prüfung ungültig sein. Admin-Reset, Sperren, Freigeben und „Sitzungen beenden“ ebenso testen.
6. „Angemeldet bleiben“: App schließen/starten; bei erreichbarem Server gültiges Konto wieder öffnen. Tokenfile `auth-session.bin` ist DPAPI-geschützt. Normales Abmelden löscht es. Adminmodus darf keinen dauerhaften Nachweis anlegen. Im Self-Test wird der native Windows-Tokenrundlauf geprüft.
7. Testentwurf mit Konto A; wechseln zu B. B darf A nicht angezeigt bekommen. Zurück zu A → Wiederherstellung anbieten. Admin darf A auf demselben PC öffnen. Auf anderem PC entsteht keine Fernübertragung des Entwurfs.
8. Alte 1.2-Wiederherstellung: ausschließlich als Admin und nach expliziter Zuordnung übernehmen. Abbruch darf die alte Datei nicht löschen. Bereits vorhandenen neuen Profilentwurf nicht überschreiben.
9. Internet trennen bzw. Server stoppen: regelmäßige Prüfung im Hauptfenster sperrt Bearbeitung. „Erneut anmelden“ bleibt verfügbar. Noch nicht gespeicherte Entwürfe dürfen nicht verschwinden. Wiederverbinden → anmelden und wiederherstellen.
10. Speicherdialog öffnen, währenddessen Sitzung auf zweitem PC widerrufen; Dateiauswahl bestätigen. Export muss durch erneute Prüfung verhindert werden, Zieldatei darf nicht geschrieben werden.
11. Gültiger Export: Vorname/Nachname in Zeile 3, korrekter Dateinamenvorschlag, Makros und zehn Kundenspalten erhalten. Andere Beraterdatei importieren → neuer Export trägt aktuellen Berater. Keine Formeleinschleusung über Namen möglich.
12. Einrichten/Update mit echten Windows-Berechtigungen testen. Bei 100/125/150-%-Skalierung Anmeldung, Passwortfenster und Adminverwaltung prüfen.
13. Server neu deployen: Konten bleiben bestehen. Admin-Hash austauschen: alte Adminsitzungen ungültig, normale Konten weiter nutzbar. Datenbanksicherung und Wiederherstellung auf Testdienst prüfen.

14. Anmeldung zeigt genau Vorname/Nachname/Passwort, keine Serveradresse und keine Admincheckbox. Alte auth-server.txt mit anderer Adresse darf keinen Einfluss haben. Neues Installationsprofil verbindet sich automatisch zum festen Server.
15. Anzeigen/Verbergen und automatisches Verdecken beim Fensterwechsel testen. Leeres Passwort und nur ein ausgefülltes Namensfeld müssen verständlich abgewiesen werden.
16. Admin mit gesetztem „Angemeldet bleiben“ anmelden, schließen und neu starten: keine automatische Adminanmeldung. Berater mit Merken darf wiederhergestellt werden. Server zuerst aktualisieren; alter Server muss vor dem Senden von Zugangsdaten einen Updatehinweis auslösen.
