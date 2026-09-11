# Spec: Authentication and Authorization

## Overview
Authentication SHALL secure staff access to the HMS, while authorization SHALL enforce role-based permissions and audit every material action. The capability SHALL support operational roles including Admin, Doctor, Nurse, Receptionist, and LabTech.

## Functional Requirements

### FR-001: Staff login
The system MUST authenticate staff with a username and password and MUST establish a session only after valid credentials are verified.

**Given** an active staff account  
**When** the user submits valid credentials  
**Then** the system MUST create an authenticated session with the user's roles and permissions.

### FR-002: RBAC
The system MUST enforce role-based access control for every protected operation and MUST support Admin, Doctor, Nurse, Receptionist, LabTech, and extensible future roles.

**Given** an authenticated user with a defined role  
**When** the user requests a protected operation  
**Then** the system MUST allow or deny it according to the assigned permission set.

### FR-003: Session management
The system MUST expire inactive sessions, support explicit logout, and MUST invalidate sessions when an account is disabled or credentials are reset.

**Given** an authenticated session  
**When** it expires, is explicitly ended, or is invalidated  
**Then** protected requests MUST be rejected until the user authenticates again.

### FR-004: Password reset
The system MUST provide a controlled password-reset flow that verifies the user's identity, enforces password policy, and prevents reuse of reset tokens.

**Given** a valid reset request  
**When** the user completes identity verification and submits a compliant password  
**Then** the system MUST update credentials, invalidate prior sessions, and consume the reset token.

### FR-005: Audit logging
The system MUST log who performed what action, when, and against which relevant record, including authentication and authorization failures.

**Given** a material operation or security event  
**When** it occurs  
**Then** the system MUST append a tamper-evident audit entry accessible only to authorized roles.

## Non-Functional Requirements
- NFR-001: Passwords MUST NOT be stored or logged in plaintext.
- NFR-002: Security-sensitive actions MUST be auditable in accordance with privacy regulations and retention policy.

## Acceptance Criteria
- [ ] Each listed operational role has enforceable permissions.
- [ ] Logout, expiry, disablement, and reset invalidate access as specified.
- [ ] Audit entries include actor, action, target, timestamp, and outcome.

## Edge Cases
- Invalid credentials: deny access without revealing whether the username exists.
- Disabled account with valid password: deny login and record the event.
- Unauthorized operation: deny the request and record the failure without exposing protected data.
