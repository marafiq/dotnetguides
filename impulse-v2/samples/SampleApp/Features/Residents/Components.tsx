import React from 'react';
import { Link } from '@tanstack/react-router';
import {
  Card,
  Badge,
  Button,
  ButtonGroup,
  Heading,
  Text,
  Divider,
  Avatar,
  ActionButton,
  StatusLight,
} from '@react-spectrum/s2';
import type {
  ListResidentsResponse,
  GetResidentResponse,
  ResidentSummary,
  Address,
  EmergencyContact,
} from '../../generated/types';
import {
  getResidentPath,
  listMedicationsPath,
  getCarePlanPath,
} from '../../generated/routes';

// ========================================
// Residents List Component
// ========================================

interface ResidentsListProps {
  data: ListResidentsResponse;
}

export function ResidentsList({ data }: ResidentsListProps) {
  const statusCounts = {
    active: data.residents?.filter(r => r.status === 'Active').length || 0,
    hospitalized: data.residents?.filter(r => r.status === 'Hospitalized').length || 0,
    onLeave: data.residents?.filter(r => r.status === 'OnLeave').length || 0,
    discharged: data.residents?.filter(r => r.status === 'Discharged').length || 0,
  };

  return (
    <div className="app-main">
      <div className="page-header">
        <div>
          <Heading level={1}>Residents Directory</Heading>
          <Text>Manage and view all residents in the facility</Text>
        </div>
        <Button variant="accent">+ Add Resident</Button>
      </div>

      {/* Stats Row */}
      <div className="impulse-flex impulse-gap-4 impulse-wrap impulse-mb-6">
        <StatCard label="Active" value={statusCounts.active} variant="positive" />
        <StatCard label="Hospitalized" value={statusCounts.hospitalized} variant="notice" />
        <StatCard label="On Leave" value={statusCounts.onLeave} variant="informative" />
        <StatCard label="Discharged" value={statusCounts.discharged} variant="neutral" />
      </div>

      {/* Residents Grid */}
      <div className="card-grid">
        {data.residents?.map((resident) => (
          <ResidentCard key={resident.id} resident={resident} />
        ))}
      </div>

      {data.residents?.length === 0 && (
        <div className="empty-state">
          <Heading level={3}>No Residents Found</Heading>
          <Text>Add your first resident to get started.</Text>
        </div>
      )}
    </div>
  );
}

function StatCard({ label, value, variant }: {
  label: string;
  value: number;
  variant: 'positive' | 'negative' | 'notice' | 'informative' | 'neutral';
}) {
  return (
    <Card UNSAFE_className="impulse-stat-card">
      <div className="impulse-flex impulse-flex-col impulse-items-center impulse-gap-2">
        <Text UNSAFE_className="impulse-stat-value">{value}</Text>
        <Badge variant={variant} size="S">{label}</Badge>
      </div>
    </Card>
  );
}

function ResidentCard({ resident }: { resident: ResidentSummary }) {
  const getStatusVariant = (status: string): 'positive' | 'negative' | 'notice' | 'informative' | 'neutral' => {
    switch (status) {
      case 'Active': return 'positive';
      case 'Hospitalized': return 'negative';
      case 'OnLeave': return 'notice';
      case 'Discharged': return 'neutral';
      default: return 'informative';
    }
  };

  return (
    <Link to={getResidentPath(resident.id)} className="resident-card card">
      <div className="impulse-flex impulse-flex-col impulse-gap-4">
        <div className="impulse-flex impulse-items-center impulse-gap-4">
          <Avatar
            src={`https://api.dicebear.com/7.x/initials/svg?seed=${resident.firstName}%20${resident.lastName}`}
            alt={`${resident.firstName} ${resident.lastName}`}
          />
          <div className="impulse-flex impulse-flex-col impulse-gap-1 impulse-flex-1">
            <Heading level={3} UNSAFE_style={{ margin: 0 }}>
              {resident.firstName} {resident.lastName}
            </Heading>
            <StatusLight variant={getStatusVariant(resident.status)}>
              {resident.status}
            </StatusLight>
          </div>
        </div>

        <Divider />

        <div className="impulse-flex impulse-justify-between">
          <div className="impulse-flex impulse-flex-col impulse-gap-1">
            <Text UNSAFE_className="impulse-label">Room</Text>
            <Text UNSAFE_className="impulse-value">{resident.roomNumber || 'Unassigned'}</Text>
          </div>
          <div className="impulse-flex impulse-flex-col impulse-gap-1 impulse-items-end">
            <Text UNSAFE_className="impulse-label">Age</Text>
            <Text UNSAFE_className="impulse-value">{resident.age} years</Text>
          </div>
        </div>
      </div>
    </Link>
  );
}

// ========================================
// Resident Detail Component
// ========================================

interface ResidentDetailProps {
  data: GetResidentResponse;
}

export function ResidentDetail({ data }: ResidentDetailProps) {
  const age = calculateAge(data.dateOfBirth);

  const getStatusVariant = (status: string): 'positive' | 'negative' | 'notice' | 'informative' | 'neutral' => {
    switch (status) {
      case 'Active': return 'positive';
      case 'Hospitalized': return 'negative';
      case 'OnLeave': return 'notice';
      default: return 'informative';
    }
  };

  return (
    <div className="app-main">
      {/* Header */}
      <div className="page-header">
        <div>
          <Link to="/residents" className="back-link">← Back to Residents</Link>
          <div className="impulse-flex impulse-items-center impulse-gap-6">
            <Avatar
              src={`https://api.dicebear.com/7.x/initials/svg?seed=${data.firstName}%20${data.lastName}&size=80`}
              alt={`${data.firstName} ${data.lastName}`}
            />
            <div>
              <Heading level={1} UNSAFE_style={{ margin: 0 }}>
                {data.firstName} {data.lastName}
              </Heading>
              <div className="impulse-flex impulse-gap-2 impulse-mt-2">
                <StatusLight variant={getStatusVariant(data.status)}>{data.status}</StatusLight>
                <Badge variant="informative" size="S">Room {data.roomNumber || 'TBD'}</Badge>
              </div>
            </div>
          </div>
        </div>
        <ButtonGroup>
          <ActionButton>Edit Profile</ActionButton>
          <Button variant="primary">Record Vitals</Button>
        </ButtonGroup>
      </div>

      {/* Content Grid */}
      <div className="detail-grid">
        {/* Basic Information */}
        <Card UNSAFE_className="impulse-card">
          <Heading level={2}>Basic Information</Heading>
          <Divider />
          <div className="info-list">
            <InfoRow label="Date of Birth" value={`${formatDate(data.dateOfBirth)} (${age} years old)`} />
            <InfoRow label="Room Number" value={data.roomNumber || 'Unassigned'} />
            <InfoRow label="Admission Date" value={formatDate(data.admissionDate)} />
            <InfoRow label="Medical Record #" value={`MRN-${String(data.id).padStart(6, '0')}`} />
          </div>
        </Card>

        {/* Address */}
        {data.address && (
          <Card UNSAFE_className="impulse-card">
            <Heading level={2}>Address</Heading>
            <Divider />
            <AddressDisplay address={data.address} />
          </Card>
        )}

        {/* Emergency Contacts */}
        <Card UNSAFE_className="impulse-card">
          <div className="impulse-flex impulse-items-center impulse-justify-between">
            <Heading level={2} UNSAFE_style={{ margin: 0 }}>Emergency Contacts</Heading>
            <Badge variant="neutral" size="S">{data.emergencyContacts?.length || 0}</Badge>
          </div>
          <Divider />
          {data.emergencyContacts && data.emergencyContacts.length > 0 ? (
            <div className="contact-list">
              {data.emergencyContacts.map((contact, i) => (
                <ContactCard key={i} contact={contact} isPrimary={i === 0} />
              ))}
            </div>
          ) : (
            <Text UNSAFE_className="impulse-muted">No emergency contacts on file.</Text>
          )}
        </Card>

        {/* Care Preferences */}
        {data.preferences && (
          <Card UNSAFE_className="impulse-card">
            <Heading level={2}>Care Preferences</Heading>
            <Divider />
            <div className="info-list">
              {data.preferences.dietaryRestrictions && (
                <InfoRow label="Dietary Restrictions" value={data.preferences.dietaryRestrictions} />
              )}
              {data.preferences.mobilityAids && (
                <InfoRow label="Mobility Aids" value={data.preferences.mobilityAids} />
              )}
              {data.preferences.communicationPreferences && (
                <InfoRow label="Communication" value={data.preferences.communicationPreferences} />
              )}
              <InfoRow
                label="Preferred Care Time"
                value={
                  [
                    data.preferences.prefersMorningCare && 'Morning',
                    data.preferences.prefersEveningCare && 'Evening'
                  ].filter(Boolean).join(' & ') || 'No preference'
                }
              />
            </div>
          </Card>
        )}

        {/* Quick Actions - Using generated path builders */}
        <Card UNSAFE_className="impulse-card impulse-full-width">
          <Heading level={2}>Quick Actions</Heading>
          <Divider />
          <div className="impulse-flex impulse-gap-4 impulse-wrap">
            <Link to={listMedicationsPath(data.id)}>
              <Button variant="secondary">View Medications</Button>
            </Link>
            <Link to={getCarePlanPath(data.id)}>
              <Button variant="secondary">View Care Plan</Button>
            </Link>
            <Button variant="secondary">Add Note</Button>
            <Button variant="secondary">Contact Family</Button>
          </div>
        </Card>
      </div>
    </div>
  );
}

// ========================================
// Shared Sub-components
// ========================================

function InfoRow({ label, value }: { label: string; value: string }) {
  return (
    <div className="info-row">
      <span className="label">{label}</span>
      <span className="value">{value}</span>
    </div>
  );
}

function AddressDisplay({ address }: { address: Address }) {
  return (
    <address className="address">
      <Text UNSAFE_className="impulse-value">{address.street}</Text>
      <br />
      <Text>{address.city}, {address.state} {address.zipCode}</Text>
    </address>
  );
}

function ContactCard({ contact, isPrimary }: { contact: EmergencyContact; isPrimary: boolean }) {
  return (
    <Card UNSAFE_className={`impulse-contact-card ${isPrimary ? 'impulse-contact-primary' : ''}`}>
      <div className="impulse-flex impulse-flex-col impulse-gap-2">
        <div className="impulse-flex impulse-items-center impulse-gap-2">
          <Text UNSAFE_className="impulse-value">{contact.name}</Text>
          <Badge variant={isPrimary ? 'positive' : 'neutral'} size="S">
            {isPrimary ? 'Primary' : contact.relationship}
          </Badge>
        </div>
        <div className="impulse-flex impulse-gap-4">
          <Text UNSAFE_className="impulse-link">{contact.phone}</Text>
          {contact.email && (
            <Text UNSAFE_className="impulse-link">{contact.email}</Text>
          )}
        </div>
      </div>
    </Card>
  );
}

// ========================================
// Utilities
// ========================================

function calculateAge(dateOfBirth: string): number {
  const dob = new Date(dateOfBirth);
  const today = new Date();
  let age = today.getFullYear() - dob.getFullYear();
  const monthDiff = today.getMonth() - dob.getMonth();
  if (monthDiff < 0 || (monthDiff === 0 && today.getDate() < dob.getDate())) {
    age--;
  }
  return age;
}

function formatDate(dateString: string): string {
  return new Date(dateString).toLocaleDateString('en-US', {
    year: 'numeric',
    month: 'long',
    day: 'numeric',
  });
}
