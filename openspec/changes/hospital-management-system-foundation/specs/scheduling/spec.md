# Spec: Scheduling

## Overview
Scheduling SHALL manage professional agendas, specialty-based time slots, rooms, and appointments (turnos) for in-person and teleconsultation workflows. It SHALL enforce appointment duration, availability, and lifecycle transitions.

## Functional Requirements

### FR-001: Professional agendas and slots
The system MUST allow authorized staff to define a professional agenda by specialty, working intervals, appointment duration, and available rooms or offices.

**Given** a professional and specialty configuration  
**When** staff publishes an agenda  
**Then** the system MUST expose non-overlapping bookable time slots with the configured duration.

### FR-002: Appointment reservation
The system MUST reserve an available slot for a patient and record the professional, specialty, location, modality, and appointment duration.

**Given** an available slot and registered patient  
**When** an authorized user reserves a turno  
**Then** the system MUST create it with status Reserved and prevent competing reservations.

### FR-003: Appointment lifecycle
The system MUST support the transitions Reserved → Confirmed → Attended, and Reserved or Confirmed → Cancelled or NoShow, subject to authorization and policy.

**Given** an appointment in a valid source state  
**When** staff performs a lifecycle action  
**Then** the system MUST apply the valid target status and record who and when changed it.

### FR-004: Modality and allocation
The system MUST distinguish teleconsultation from in-person appointments and MUST allocate a room or office for in-person appointments.

**Given** a requested appointment modality  
**When** the booking is confirmed  
**Then** the system MUST require an available room for in-person care and a valid teleconsultation destination for remote care.

## Non-Functional Requirements
- NFR-001: The system MUST prevent double booking of a professional, room, or time slot.
- NFR-002: Agenda changes MUST be auditable and MUST NOT erase completed appointment history.

## Acceptance Criteria
- [ ] Slots vary by specialty and professional duration configuration.
- [ ] Every appointment has one permitted lifecycle status.
- [ ] In-person and teleconsultation bookings enforce their distinct allocation rules.

## Edge Cases
- Concurrent booking of one slot: exactly one request succeeds; others receive a conflict.
- Cancelled appointment: its slot MAY become bookable again without changing its historical record.
- Invalid status transition: reject the action and retain the current status.
