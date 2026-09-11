# Explore: Hospital Management System Domain

## Executive Summary

This exploration maps the clinical and administrative domain of the **Sanatorio Adventista del Plata (SAP)**, a non-profit, high-complexity Adventist hospital in Entre Ríos, Argentina — the reference institution for the greenfield Hospital Management System. SAP is a 118-year-old (founded 1908), full-lifecycle care provider ("from gestation to old age") serving ~200,000 outpatient consultations per year with ~180 listed professionals across ~85 specialty/subspecialty entries (the institution claims 55 medical specialties), ~86 diagnostic studies/procedures, a 24/7 emergency service using the Manchester triage protocol, five hospitalization areas (clinical/surgical, ICU, cardiovascular recovery, mental health, neonatology), plus pharmacy, blood bank, telemedicine, and lifestyle-medicine programs. It is ITAES-accredited (2026–2029) and part of a worldwide network of 2,000+ Adventist health centers.

The domain is a classic mid-to-large acute-care hospital with strong Argentine regulatory constraints: health-data privacy (Ley 25.326 + Disposición 60-E/2016), patient rights and medical-record rules (Ley 26.529), mental-health law (Ley 26.657), and electronic-prescription law (Ley 27.553). The system under design (Clean Architecture + DDD + CQRS, .NET 8, EF Core 8, SQL Server 2022, WPF desktop) must model episodes of care, appointments, encounters, orders, notes, billing/coding for Argentine payers (obras sociales, prepagas, PAMI, mutuales, private), and an existing patient portal (`portalpaciente.sanatorioadventista.org.ar`) that already handles appointment booking ("Sacar Turno").

## Institutional Profile

- **Organization type**: Non-profit (sin fines de lucro), faith-based (Seventh-day Adventist), private hospital; part of a worldwide network of 2,000+ Adventist health centers (150-year philosophy).
- **History**: Founded November 1908 by Dr. Robert Habenicht (arrived Dec 1901, first Adventist medical institution in South America); 118 years of uninterrupted service.
- **Location**: Puiggari / Libertador San Martín area, Entre Ríos, Argentina. Phone/WhatsApp: +54 343 4200220; Emergency: 0343 4200 250.
- **Governance**: Director General (Dr. Haroldo Steger), Director Médico (Dr. Edson Velozo), Director Financiero (Prof. Arnoldo Schlemper). Service chiefs ("Jefe de ...") per department (Cardiología, Cirugía, Clínica, UTI, Emergencia, Oftalmología, Odontología, Ortopedia, Ginecología, Nutrición, Fisioterapia, Patología, Medicina Nuclear, Medicina Física, Diagnóstico por Imágenes, Urología, Neonatología, Oncología, Bienestar Mental).
- **Accreditation**: ITAES (Instituto Técnico para la Acreditación de Establecimientos de Salud) — badge displayed for 2026–2029.
- **Key services**: Cardiología, Cirugía, Clínica Médica, Estudio Diagnóstico (imágenes/laboratorio), Medicina Física y Rehabilitación, Ginecología y Obstetricia, Neonatología y Pediatría, Nutrición, Oftalmología, Odontología, Bienestar Mental, Banco de Sangre, Emergencias (Guardia 24/7), Capellanía, Internación, Kinesiología y Fisioterapia, Medicina del Estilo de Vida (MEV), Anestesia, Ortopedia y Traumatología.
- **Additional services**: Farmacia, Óptica, Comedor, Geriátrico; sister institutions Ephicient (clínica) and UAP (Universidad Adventista del Plata); Docencia; Vida Sana / Plan de 21 días (prevention programs).
- **Scale indicators**:
  - ~200,000 annual outpatient consultations (≈550/day).
  - ~180 professionals listed on the website (3 paginated pages), 8 professions: Médico, Odontólogo, Psicólogo, Nutricionista, Kinesiólogo, Fonoaudiólogo, Terapista Ocupacional, Psicopedagogo.
  - ~85 specialty/subspecialty entries listed; institution claims "55 especialidades médicas" (site also says "más de 40" — inconsistent; 85 is the operational catalog).
  - ~86 diagnostic studies/procedures catalogued with preparation requirements and prior-authorization rules.
  - 24/7 emergency with Manchester triage; 5 hospitalization areas; international patients.
  - ~20 administrative/clinical departments with published hours, direct lines, and WhatsApp channels.

## Domain Model Analysis

### Core Entities

| Entity | Notes |
|---|---|
| **Patient (Paciente)** | Demographics, contact, legal guardian (pediatrics), coverage(s), emergency contact; full-lifecycle (gestation → geriatrics); minors frequent (pediatría, neonatología, Urología Pediátrica). |
| **Professional (Profesional)** | Identity (matrícula MP/ME), profession type (8 types), specialties (many-to-many — e.g., Bernhardt Leal: Cirugía General + Endoscopía + Gastroenterología), leadership role (Jefe de servicio), schedule/availability. |
| **Specialty (Especialidad)** | ~85-entry catalog; parent/child structure exists (Cardiología → Cardiología Nuclear, Electrofisiología; Pediatría → Nefrología Infantil, Neumonología Infantil, Reumatología Infantil, Gastroenterología Infantil, Urología Pediátrica). |
| **Study/Procedure (Estudio)** | ~86-entry catalog; type clusters (Endoscopía, ECO Doppler, Ecografía, Tomografía, Resonancia, Medicina Nuclear, Cardiología no invasiva, Oftalmología, Alergia, Neurología); requires prior authorization from payer and preparation instructions. |
| **Appointment (Turno)** | Booked via portal, WhatsApp, phone, or in-person "turneras"; per-department schedules; many services "sujeto a turneras médicas" (secretary-mediated); studies require authorization before booking. |
| **Encounter (Atención)** | Ambulatory consult, emergency visit, hospitalization episode; multi-professional per episode. |
| **Episode of Care** | Emergency episode (triage → stabilization → admit/refer), hospitalization (clinical/surgical, UTI, cardiovascular recovery, mental health, neonatology), chronic-care episode (diabetes, tobacco, obesity, oncología). |
| **Order (Indicación)** | Lab, imaging, studies, procedures; drives billing and scheduling; authorization workflow against payer. |
| **Medical Record (Historia Clínica)** | Registros Médicos department (fotocopia.historia@...); regulated by Ley 26.529; records requests/copies handled administratively. |
| **Coverage / Payer** | Obra social, prepaga, PAMI/INSSJP, mutual, particular/private, international; authorization and coding per payer. |
| **Department/Service (Servicio)** | ~20 units with hours, direct phone, WhatsApp, email; drives scheduling and staffing. |
| **Admission/Discharge (Admisión/Egresos)** | Separate administrative unit with its own hours (Sun 8–16, Mon–Thu 7:30–19, Fri 8–15). |
| **Billing/Coding (Codificación/Facturación)** | "Codificación" unit (mutuales@...) codes practices for reimbursement; Asuntos Financieros handles payments/collections. |
| **Inventory/Pharmacy** | Farmacia (outpatient + inpatient dispensing), Banco de Sangre (donors, units, hemoterapia), Óptica. |
| **Donor** | Blood donor lifecycle (Banco de Sangre: "Donar Sangre"). |

### Key Workflows

1. **Appointment booking (Turnos)** — multi-channel (portal / WhatsApp / phone / turneras); secretary-mediated for many specialties; authorization check for studies; per-department calendar rules (e.g., Cardiología Mon–Thu 8–18, Fri/Sun 8–14).
2. **Emergency care (Guardia)** — 24/7; **Manchester triage protocol**; evaluation/stabilization; urgent lab + imaging; coordination with UTI and hospitalization; referral to specialties; admission decisions.
3. **Ambulatory encounter** — consult → orders (studies/lab) → authorization → scheduling → results → follow-up; telemedicine channel exists (Telemedicina dept, Mon–Thu 8–15).
4. **Hospitalization** — admission (Admisión/Egresos) → stay in one of 5 areas → discharge; surgical coordination (Coordinación de Cirugías Traumatológicas); ICU for critical patients.
5. **Diagnostic studies** — order → payer authorization → booking (with preparation instructions) → execution (equipment: MRI 1.5T, CT 64/128, mammography, gamma camera) → report → result delivery.
6. **Billing & reimbursement** — practice coding (Nomenclador/NOMIVARC), claims to obras sociales/prepagas/mutuales, financial affairs for private billing, collections.
7. **Blood bank** — donation → screening (Hemoterapia e Inmunohematología) → storage → transfusion.
8. **Pharmacy** — dispensing; inpatient medication administration ("Administración controlada de medicación" listed as a study/service).
9. **Patient portal self-service** — appointments, results, preparation info (existing external system to integrate with or replace).
10. **Prevention programs** — MEV, Plan de 21 días, Vida Sana, Tabaquismo, Obesity — lifestyle-medicine care plans (adjacent to core clinical flow).

### Bounded Contexts

| Bounded Context | Responsibility | Key entities |
|---|---|---|
| **Scheduling (Turnos)** | Multi-channel appointment booking, calendars, authorization gating | Appointment, Department, Professional schedule, Portal integration |
| **Patient Registry** | Demographics, coverage, guardianship, consent | Patient, Coverage, Legal guardian |
| **Clinical Care** | Encounters, notes, orders, episodes, medical record | Encounter, Episode, Order, Clinical Note, Historia Clínica |
| **Emergency** | 24/7 triage (Manchester), stabilization, admission/referral | Triage, Emergency Episode, Observation |
| **Hospitalization** | Bed management, stays, ward areas, discharge | Admission, Stay, Bed, Ward (5 areas), Discharge |
| **Diagnostics** | Study catalog, orders, results, reports, equipment scheduling | Study, Result, Report, Modality (imaging/lab/endoscopy/nuclear) |
| **Pharmacy & Blood Bank** | Inventory, dispensing, blood units, hemotherapy | Medication, Stock, Blood Unit, Donor |
| **Billing & Coverage** | Coding, claims, authorizations, collections | Claim, Authorization, Payer, Coding record |
| **Access & Portal** | Patient self-service, identity, results publication | Portal account, Appointment, Result |
| **Administration & Quality** | Staff, departments, ITAES accreditation, docencia | Professional, Department, Accreditation evidence |

## Technical Implications

- **Architecture fit**: The domain is a textbook DDD aggregate cluster — Clean Architecture + CQRS + MediatR (per `openspec/config.yaml`) maps well to the bounded contexts above. Recommend a modular monolith first, with `Scheduling`, `ClinicalCare`, `Diagnostics`, `Billing`, and `Access` as the first module slices; Emergency and Hospitalization as high-risk second slices.
- **Identity & coverage**: Argentine payers are heterogeneous (obras sociales, prepagas, PAMI, mutuales, private, international). Coverage/authorization must be first-class (not an afterthought) — it gates scheduling and billing.
- **Terminology**: Domain vocabulary is Spanish (turnos, guardia, internación, obras sociales, codificación). Use Spanish for UI strings and clinical terminology; English for code identifiers/artifacts. Map Spanish terms explicitly in a domain glossary to avoid drift.
- **Coding standards**: Plan for CIE-10/CIE-11 (diagnoses), NOMIVARC/Nomenclador Nacional (practices), SNOMED CT (clinical concepts), LOINC (lab) — at least as extension points, even if seeded with local catalogs.
- **Regulatory (Argentina)**:
  - **Ley 25.326 + Disposición 60-E/2016** (health data): consent, data minimization, database registration with AAIP, security measures for sensitive health data.
  - **Ley 26.529** (Derechos del Paciente): medical record rules, informed consent, patient rights.
  - **Ley 26.657** (Salud Mental): special rules for the Bienestar Mental area (internación constraints).
  - **Ley 27.553**: electronic prescriptions/digital signatures — pharmacy dispensing must support digital prescriptions.
  - **Manchester triage** coding for emergency; **ITAES** accreditation evidence trails for quality context.
- **Scale & availability**: ~550 consultations/day, 24/7 emergency, international patients → availability and audit-trail requirements exceed a toy system; SQL Server 2022 + EF Core 8 is adequate; plan for role-based access and full audit logging from day one.
- **Existing ecosystem**: Patient portal exists (external); "Codificación"/"Registros Médicos"/"Asuntos Financieros" imply legacy workflows — integration or staged replacement decisions are required.
- **Data source caveats**: Website data is marketing-grade: specialty count inconsistent (55 claimed vs 85 listed vs "más de 40"), several professionals show "No items found" (missing specialty data), and some catalog entries are service-likes ("Administración controlada de medicación"). Treat the website as a seed catalog, not a canonical schema.

## Risks and Opportunities

### Risks
- **Scope explosion**: 85+ specialties, 86+ studies, ~180 professionals, 10 workflows — the greatest risk is modeling everything up front. Mitigation: MVP bounded to Scheduling + Patient Registry + Clinical Care (ambulatory) + Diagnostics orders/results; other contexts as later slices.
- **Regulatory exposure**: Health-data privacy (Ley 25.326 / Disp. 60-E/2016) and medical-record law (Ley 26.529) are non-negotiable; breach = legal + reputational damage. Mitigation: consent model, RBAC, audit logs, data-residency (Argentina), AAIP registration step in rollout plan.
- **Legacy integration**: Existing patient portal and manual codificación/mutuales workflows may resist automation; unclear APIs. Mitigation: portal integration adapter + manual-workflow support in early versions.
- **Data quality**: Website-derived catalogs contain inconsistencies ("No items found", count mismatches, service-like entries). Mitigation: validate catalogs with domain experts before seeding production data.
- **Bilingual drift**: Spanish clinical terms vs English codebase can diverge. Mitigation: explicit domain glossary artifact + Spanish UI resource layer.

### Opportunities
- **First-mover greenfield**: No legacy code to constrain architecture; the modular-monolith + CQRS design can be done right from the start.
- **Full-lifecycle care**: The "gestación a ancianidad" model is a differentiator — pediatrics + geriatrics + lifestyle-medicine programs (MEV, Plan 21 días) are natural value-adds beyond basic HMS features.
- **Accreditation leverage**: ITAES requirements provide a ready-made quality checklist for audit-trail and safety features.
- **Billing automation**: Codificación is manual today — automated coding/claims against NOMIVARC is high-value and low-competition.
- **Prevention programs**: Lifestyle medicine (MEV, Tabaquismo, Obesidad) can become care-plan templates — a distinctive module few HMS products offer.

## Ready for Proposal

Yes — sufficient domain understanding for `sdd-propose`. The orchestrator should present the bounded-context map and MVP slice (Scheduling, Patient Registry, Clinical Care, Diagnostics) to the user, and confirm: (a) target users (internal staff via WPF vs patient portal), (b) whether to integrate or replace the existing patient portal, and (c) MVP scope acceptance (which bounded contexts are in/out).