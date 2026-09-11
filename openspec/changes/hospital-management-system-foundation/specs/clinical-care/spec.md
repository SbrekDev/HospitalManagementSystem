# Spec: Clinical Care

## Overview
Clinical care SHALL manage ambulatory episodes, professional assignment, clinical encounters, notes, and care orders. It SHALL preserve an attributable medical record for each episode and support closure only after required work is complete.

## Functional Requirements

### FR-001: Episode creation and assignment
The system MUST create an ambulatory episode for a patient and assign a responsible professional, with status Active.

**Given** a registered patient and authorized professional  
**When** staff starts an ambulatory encounter  
**Then** the system MUST create an Active episode linked to the patient and assigned professional.

### FR-002: Clinical notes
The system MUST record attributable, time-stamped evolution notes and procedure notes within an active episode.

**Given** an active episode and authorized professional  
**When** the professional saves a clinical note  
**Then** the system MUST persist the note with author, timestamp, note type, and episode reference.

### FR-003: Care orders
The system MUST allow authorized professionals to create medication, study, procedure, and diet orders within an active episode.

**Given** an active episode  
**When** a professional issues a valid order  
**Then** the system MUST record its type, content, author, date, and patient association for downstream fulfillment.

### FR-004: Episode closure
The system MUST transition an episode from Active to Closed only when closure requirements are satisfied and MUST prevent unauthorized edits to closed clinical content.

**Given** an active episode with required documentation complete  
**When** an authorized professional closes it  
**Then** the system MUST mark it Closed and preserve its notes and orders as historical records.

## Non-Functional Requirements
- NFR-001: Clinical entries MUST be immutable after finalization; corrections MUST be traceable additions.
- NFR-002: Access MUST comply with least privilege and medical-record confidentiality requirements.

## Acceptance Criteria
- [ ] Episodes link one patient, status, and responsible professional.
- [ ] Notes distinguish evolution from procedure documentation.
- [ ] All four order types are supported and attributable.

## Edge Cases
- Closed episode edit: reject direct mutation and require a compliant correction workflow.
- Missing professional assignment: reject episode creation.
- Duplicate submission: do not create duplicate notes or orders.
