# Spec: Patient Registry

## Overview
The patient registry SHALL maintain a unique, searchable record for each patient, including identity, contact information, medical history, coverage, and emergency or family relationships. It SHALL support staff workflows for registration, retrieval, and controlled updates.

## Functional Requirements

### FR-001: Patient creation and identity
The system MUST create a patient with name, surname, date of birth, gender, contact data, and one document of type DNI, LC, LE, or Passport.

**Given** valid required data and an unused document number  
**When** an authorized user creates a patient  
**Then** the system MUST persist the patient and return a unique patient identifier.

### FR-002: Patient retrieval, update, and search
The system MUST allow authorized users to retrieve and update patient data and search by identifier, document, name, surname, or contact data.

**Given** an existing patient  
**When** a user searches or updates permitted fields  
**Then** the system MUST return the matching record or persist the validated changes.

### FR-003: Medical history and coverage
The system MUST record allergies, comorbidities, medications, and coverage type: obra social, prepaga, or particular, including payer details when applicable.

**Given** an existing patient  
**When** an authorized user records history or coverage  
**Then** the system MUST associate the information with the patient and preserve its audit trail.

### FR-004: Family and emergency relationships
The system MUST record a related person, relationship, contact data, and guardianship status when required, especially for minors.

**Given** a minor or a patient requiring an emergency contact  
**When** staff registers a family relationship  
**Then** the system MUST store the relationship and identify the authorized guardian or contact.

## Non-Functional Requirements
- NFR-001: Patient data MUST be access-controlled and auditable in accordance with applicable privacy and medical-record obligations.
- NFR-002: Search SHOULD return normal registry queries within 2 seconds under expected MVP load.

## Acceptance Criteria
- [ ] Duplicate document numbers are rejected.
- [ ] All four document types are accepted and searchable.
- [ ] History, coverage, and guardian data are visible only to authorized staff.

## Edge Cases
- Duplicate identity document: reject creation and identify the existing record.
- Missing payer for obra social/prepaga: reject the incomplete coverage entry.
- Minor without guardian data: allow draft registration only if policy permits; otherwise reject completion.
