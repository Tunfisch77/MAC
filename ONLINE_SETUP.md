# Ab Version 1.3: zusätzliche zentrale Anmeldung

**Die vollständige Reihenfolge steht in EINRICHTUNG_1.3.2.md.** Zuerst den Server aus `auth-server/` über das mitgelieferte `render.yaml` bereitstellen, Admin-Hash nur beim Hoster hinterlegen, Konten anlegen und testen. Erst danach das neue GitHub-Release veröffentlichen. Die folgenden Hinweise erläutern den unveränderten Mechanismus des Online-Installers.

# Online-Installer 1.3.2 – zuerst GitHub bereitstellen

Die Berater erhalten nur `MAC-Setup-1.3.2.exe`. Das Setup lädt alle Programmdateien aus dem öffentlichen Repository `Tunfisch77/MAC`. Es zeigt seinen Zustand und Fehler im Fenster an und kann erneut zur Reparatur ausgeführt werden. Die eigentliche App läuft anschließend lokal; die zentrale Anmeldung, ihre regelmäßige Überprüfung und automatische und manuelle Updateprüfungen benötigen Internet.

## Einmalige Veröffentlichung durch den Administrator

1. Das Administratorpaket vollständig entpacken.
2. Den Inhalt von `Quellcode/MAC-App` in die Wurzel des Repositorys hochladen. Keine ursprüngliche Kundendatei hochladen.
3. Unter https://github.com/Tunfisch77/MAC/releases/new ein öffentliches, reguläres Release mit dem Tag `v1.3.2` erstellen.
4. Genau diese drei Dateien aus `Release-Dateien` als Release-Assets anhängen: `MAC-Setup-1.3.2.exe`, `MAC-Programm-1.3.2.zip`, `SHA256SUMS.txt`.
5. Release veröffentlichen. Das Programm-ZIP unverändert hochladen, nicht neu packen oder umbenennen. Es genügt nicht, das ZIP als normale Repositorydatei hochzuladen.
6. Setup auf einem Windows-PC testen. Erst danach ausschließlich die Setup-EXE an Berater weitergeben.

Bis Schritt 5 meldet das Setup fehlende Dateien. Die Veröffentlichung von 1.3.2 wurde von hier nicht durchgeführt. Vorhandene ältere Releases enthalten nicht das für dieses Setup erforderliche Paket.

## Sicherheit und Installation

Das Setup enthält den SHA-256 des passenden Programmpakets fest im Programmcode. Es akzeptiert nur HTTPS-Downloads aus dem angegebenen Repository und offizielle GitHub-Release-Weiterleitungen. Unerwartete ZIP-Dateien, unvollständige Pakete und falsche Prüfsummen werden abgewiesen. Vor dem Kopieren wird der Anwendungsselbsttest ausgeführt. Vorhandene eigene Programmdateien werden für einen Rücksetzversuch bei Kopierfehlern im RAM gesichert. Andere Dateien werden nicht gelöscht.

Installation pro Benutzer unter `%LOCALAPPDATA%\MAC-Versicherungsagentur\`, ohne Administratorrechte. Verknüpfungsfehler verhindern den App-Start nicht mehr und werden angezeigt. Nach dem Start prüft das Setup, ob die App sofort wieder beendet wurde, und zeigt gegebenenfalls den Exitcode. Fehlertexte können im Setup-Fenster markiert und kopiert werden. Es werden keine Kundenlogs angelegt. Temporär angelegte Dateien enthalten ausschließlich das geprüfte, kundenfreie Programmpaket.

## Neue Version bauen

In `src/Version.cs` die Version erhöhen und `build.ps1` auf Windows ausführen. Das Skript baut zuerst die App, erzeugt dann `MAC-Programm-VERSION.zip` und kompiliert dessen Prüfsumme in `MAC-Setup-VERSION.exe`. Diese zusammengehörigen Dateien und `SHA256SUMS.txt` gemeinsam unter `vVERSION` veröffentlichen. Der mitgelieferte Workflow erzeugt alle drei Release-Dateien ohne Veröffentlichung. Bereits veröffentlichte Pakete nicht nachträglich austauschen; eine neue Versionsnummer verwenden.

Die Update-Schaltfläche einer installierten App lädt weiterhin den neueren Setup-Installer und prüft dessen von GitHub gelieferten SHA-256. Das neue Setup lädt anschließend das zu seiner Version passende Programmpaket.

## Prüfstand und bisheriger Startfehler

Der gemeldete Fehler `System.String.TrimStart(Char)` aus 1.0.0 wurde in 1.0.1 behoben. Der Build verwendet weiterhin ausschließlich Microsoft-.NET-Framework-4.7.2-Referenzen. Eine zusätzliche Prüfung kontrolliert die tatsächlich erzeugten String-Methodenaufrufe in App und Setup.

Kompilierung, lokale Anwendungsselbsttests, Setup-Selbsttests, Prüfsumme und Paketstruktur werden vor der Übergabe geprüft. Ein tatsächlicher GitHub-Download dieses Pakets und die Windows-Installation sind erst nach Veröffentlichung und auf einem Windows-PC prüfbar. Der Installer ist unsigniert. Windows kann ihn deshalb blockieren; in diesem Fall ist der genaue Wortlaut der Windows-Meldung für die Diagnose nötig.

## Lokale Daten seit 1.3.2
Einstellungen und verschlüsselte Entwürfe liegen getrennt unter `%LOCALAPPDATA%\MAC-Versicherungsagentur-Daten\`. Das Setup ersetzt nur die fünf bekannten Programmdateien und verändert diesen Datenordner nicht. Vor dem Update fordert die App bei unexportierten Änderungen weiterhin den Excel-Export an. Der Windows-Anwendungsselbsttest prüft zusätzlich DPAPI mit temporären synthetischen Daten.
