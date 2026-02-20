🏀 NexHoop
Sistem Informatic de Management și Analiză a Performanței

(Listă de cerințe – draft de lucru)

📌 1. Scopul aplicației

NexHoop este un sistem informatic destinat unui club profesionist de baschet, care are rolul de a automatiza:

evidența jucătorilor

planificarea sezonului

monitorizarea performanței

analiza statistică

raportarea managerială

gestionarea financiară și contractuală

Roluri principale:

Manager

Antrenor

Jucător

Posibil extensibil:

Admin

Medic

Scout

⚙️ 2. Funcționalități de bază (Minim necesar)
👥 Evidență jucători

Date personale

Poziție

Categorie

Istoric echipe

Contracte:

Data semnării

Data expirării

Statistici per echipă:

Puncte

Minute jucate

Efficiency

📅 Planificare sezon

Calendar meciuri

Turnee

Cantonamente

Locații

📊 Secțiune performanță

Introducere manuală de către antrenor:

Puncte

Recuperări

Asisturi

Minute jucate

Evaluări

Statistici generate:

Per meci

Per sezon

📈 Rapoarte manageriale

Cheltuieli:

Salarii

Transferuri

Transport

Cazare

KPI echipă:

Win rate

Medii jucători

🔔 Alerte / Notificări

Expirare contract

Accidentări înregistrate

Conflicte de program

🔐 Autentificare & Autorizare

Manager

Antrenor

Jucător

(Extensibil: Admin, Medic, Scout)

📄 Generare PDF

Contracte

Raport meci

Foaie de joc

Facturi

Rapoarte progres

Export automat + personalizat.

📊 Dashboard Analytics

Grafice trend (puncte, recuperări)

Comparatoare jucători

Heatmap (zone de aruncare)

Chart library integrat

📥 Import / Export

CSV

Excel

Date jucători

Calendar

🏥 Istoric medical & Management accidentări

Perioadă estimată recuperare

Tratamente

Restricții

📑 Versionare contracte & Semnătură

Istoric versiuni contract

Semnătură electronică

Semnătură scanată

💰 Modul financiar

Buget sezon

Cheltuieli vs buget

Forecast financiar

📲 Notificări

Push

Email

SMS

📁 Upload fișiere

Video meci

Rapoarte medicale (PDF)

Contracte scanate

🖥️ Dashboard personalizat pe rol

Pagina principală diferă în funcție de rol:

Manager → KPI + Buget

Antrenor → Performanță + Lot

Jucător → Statistici personale

🔑 Two-Factor Authentication (2FA)
🚀 Funcționalități Avansate (În analiză)
🐳 Docker + Deployment

Docker

Deployment pe:

VM

Azure

AWS

💬 Sistem intern de mesagerie

Comunicare:

Manager → Antrenor

Antrenor → Jucători

Structură posibilă:

Messages

FromUserId

ToUserId

Text

SentAt

Funcționalități:

Inbox

Outbox

Notificări mesaje necitite

📆 Integrare Google Calendar

Sincronizare automată a meciurilor și antrenamentelor.

🛡️ Permisiuni avansate (ACL)

Poate modifica contracte

Poate șterge jucători

Poate genera rapoarte

Ecran admin pentru configurare permisiuni pe rol.

📝 Audit & Logging

Log pentru acțiuni critice:

Modificare contract

Modificare statistici

Cine a modificat

Ce a modificat

Când a modificat

🧪 Testare automată + CI/CD

Build automat

Teste automate

Publish automat

Pipeline (GitHub Actions / Azure DevOps)
