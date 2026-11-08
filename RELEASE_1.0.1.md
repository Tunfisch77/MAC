# MAC 1.0.1 – Windows-Kompatibilität

Bestätigte Ursache: Die 1.0.0-Binärdatei verwendete System.String.TrimStart(Char), das in der Zielplattform .NET Framework nicht verfügbar ist. Der Mono-Build hatte versehentlich gegen die erweiterten Mono-Laufzeitbibliotheken kompiliert.

Korrektur: char-Arrays bei TrimStart, TrimEnd und Split; isolierter Build mit /nostdlib und Microsoft.NETFramework.ReferenceAssemblies.net472 1.0.3. Keine Änderung an Kundenspalten, Vorlage oder Exportbedeutung. Die ausführliche Fehleranzeige des Diagnose-Setups bleibt enthalten.

Neues reguläres GitHub-Release mit TAG v1.0.1 erstellen. Die drei Dateien aus Release-Dateien anhängen: MAC-Setup-1.0.1.exe, MAC-Programm-1.0.1.zip, SHA256SUMS.txt. Das Programm-ZIP nicht entpacken oder neu packen. Danach MAC-Setup-1.0.1.exe verwenden; der alte Installer bleibt auf Version 1.0.0 festgelegt.

Prüfungen: Kompilierung gegen Microsofts net472-Referenzen; App-Selbsttests, GUI-Test mit Mehrfachauswahl, Excel-Export und Zurücksetzen; Setup-Prüfung mit fest eingebautem SHA-256. Laufzeittests finden unter Mono statt. Eine tatsächliche Ausführung auf Windows muss anschließend geprüft werden. Es wurde kein GitHub-Release veröffentlicht.

Zusätzliche Binärprüfung: Die sechs betroffenen String-Aufrufe der App und der eine String-Aufruf des Installers verwenden char[]-Überladungen. Der Regressionstest weist die alte 1.0.0-Binärdatei erwartungsgemäß zurück. Der Windows-Build führt diese Prüfung ab jetzt automatisch aus.
