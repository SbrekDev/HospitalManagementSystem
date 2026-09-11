# Spec: Diagnostics

## Overview
Diagnostics SHALL manage laboratory and imaging orders, a catalog of studies mapped to the hospital's specialties, preparation instructions, authorization gating, and validated results.

## Functional Requirements

### FR-001: Study catalog
The system MUST provide a maintainable catalog containing at least 85 specialties mapped to their applicable laboratory and imaging studies, subject to expert validation.

**Given** a specialty selected by an authorized user  
**When** the user requests available studies  
**Then** the system MUST return the validated studies applicable to that specialty.

### FR-002: Diagnostic orders
The system MUST create laboratory or imaging orders linked to a patient and clinical episode, including study, requesting professional, priority, and preparation instructions.

**Given** an active episode and valid catalog study  
**When** an authorized professional creates an order  
**Then** the system MUST persist the order and expose its preparation requirements.

### FR-003: Authorization gating
The system MUST represent authorization requirements and MUST prevent processing when a required authorization is absent or denied.

**Given** an order requiring authorization  
**When** staff attempts to process it without approval  
**Then** the system MUST block processing and identify the authorization issue.

### FR-004: Result registration and validation
The system MUST allow designated laboratory or imaging staff to register results and a qualified professional to validate them.

**Given** a fulfilled diagnostic order and result data  
**When** designated staff records and a professional validates the result  
**Then** the system MUST store the result, validation identity, timestamp, and order linkage.

## Non-Functional Requirements
- NFR-001: Results MUST be protected by role-based access and MUST retain an immutable audit history.
- NFR-002: Catalog changes SHOULD be versioned so existing orders retain their original study definition.

## Acceptance Criteria
- [ ] Laboratory and imaging orders follow the same traceable order-to-result loop.
- [ ] Preparation instructions are available before appointment or processing.
- [ ] Unvalidated results are clearly distinguished from validated results.

## Edge Cases
- Retired study with existing order: preserve the order and prevent new use.
- Rejected authorization: keep the order but block fulfillment.
- Result correction: create a traceable correction without overwriting the original result.
