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
// Residents List Component - Modern S2 Design
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
    <div className="residents-container">
      {/* Header */}
      <header className="residents-header">
        <div className="residents-header-content">
          <Heading level={1} UNSAFE_className="residents-title">Residents</Heading>
          <Text slot="description" UNSAFE_className="residents-subtitle">
            {data.residents?.length || 0} residents in the facility
          </Text>
        </div>
        <Link to="/residents/new">
          <Button variant="accent">+ Add Resident</Button>
        </Link>
      </header>

      {/* Status Overview */}
      <section className="residents-stats">
        <StatusCard icon="✓" count={statusCounts.active} label="Active" color="green" />
        <StatusCard icon="🏥" count={statusCounts.hospitalized} label="Hospitalized" color="red" />
        <StatusCard icon="📅" count={statusCounts.onLeave} label="On Leave" color="blue" />
        <StatusCard icon="📋" count={statusCounts.discharged} label="Discharged" color="gray" />
      </section>

      {/* Residents Grid */}
      <section className="residents-grid">
        {data.residents?.map((resident) => (
          <ResidentCard key={resident.id} resident={resident} />
        ))}
      </section>

      {data.residents?.length === 0 && (
        <div className="empty-state">
          <span className="empty-icon">👥</span>
          <span>No residents found</span>
          <Link to="/residents/new">
            <Button variant="primary">Add First Resident</Button>
          </Link>
        </div>
      )}
    </div>
  );
}

// Status Card Component
function StatusCard({ icon, count, label, color }: {
  icon: string;
  count: number;
  label: string;
  color: 'green' | 'red' | 'blue' | 'gray';
}) {
  const colors = {
    green: { bg: '#f0fdf4', border: '#22c55e', text: '#166534' },
    red: { bg: '#fef2f2', border: '#ef4444', text: '#991b1b' },
    blue: { bg: '#eff6ff', border: '#3b82f6', text: '#1e40af' },
    gray: { bg: '#f9fafb', border: '#6b7280', text: '#374151' },
  };
  const c = colors[color];

  return (
    <div className="status-card" style={{ backgroundColor: c.bg, borderColor: c.border }}>
      <span className="status-count" style={{ color: c.border }}>{count}</span>
      <span className="status-label" style={{ color: c.text }}>{label}</span>
    </div>
  );
}

// Resident Card Component
function ResidentCard({ resident }: { resident: ResidentSummary }) {
  const statusColors: Record<string, { bg: string; dot: string }> = {
    Active: { bg: '#dcfce7', dot: '#22c55e' },
    Hospitalized: { bg: '#fee2e2', dot: '#ef4444' },
    OnLeave: { bg: '#dbeafe', dot: '#3b82f6' },
    Discharged: { bg: '#f3f4f6', dot: '#6b7280' },
  };
  const sc = statusColors[resident.status] || statusColors.Active;

  return (
    <Link to={getResidentPath(resident.id)} className="resident-card-link">
      <div className="resident-card">
        <div className="resident-avatar">
          <Avatar
            src={`https://api.dicebear.com/7.x/initials/svg?seed=${resident.firstName}%20${resident.lastName}&backgroundColor=e0e7ff`}
            alt={`${resident.firstName} ${resident.lastName}`}
            size="L"
          />
        </div>
        <div className="resident-info">
          <span className="resident-name">{resident.firstName} {resident.lastName}</span>
          <div className="resident-status" style={{ backgroundColor: sc.bg }}>
            <span className="resident-status-dot" style={{ backgroundColor: sc.dot }} />
            <span>{resident.status}</span>
          </div>
        </div>
        <div className="resident-details">
          <div className="resident-detail">
            <span className="detail-label">Room</span>
            <span className="detail-value">{resident.roomNumber || '—'}</span>
          </div>
          <div className="resident-detail">
            <span className="detail-label">Age</span>
            <span className="detail-value">{resident.age}</span>
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

  return (
    <div className="resident-detail-container">
      {/* Back Link */}
      <Link to="/residents" className="back-link">
        ← Back to Residents
      </Link>

      {/* Profile Header */}
      <header className="resident-profile-header">
        <div className="profile-avatar">
          <Avatar
            src={`https://api.dicebear.com/7.x/initials/svg?seed=${data.firstName}%20${data.lastName}&backgroundColor=e0e7ff&size=120`}
            alt={`${data.firstName} ${data.lastName}`}
          />
        </div>
        <div className="profile-info">
          <Heading level={1} UNSAFE_className="profile-name">
            {data.firstName} {data.lastName}
          </Heading>
          <div className="profile-badges">
            <Badge variant={data.status === 'Active' ? 'positive' : 'notice'} size="M">
              {data.status}
            </Badge>
            <Badge variant="informative" size="M">Room {data.roomNumber || 'TBD'}</Badge>
            <Badge variant="neutral" size="M">{age} years old</Badge>
          </div>
        </div>
        <div className="profile-actions">
          <Button variant="secondary" fillStyle="outline">Edit Profile</Button>
          <Button variant="accent">Record Vitals</Button>
        </div>
      </header>

      {/* Content Grid */}
      <div className="resident-content-grid">
        {/* Info Card */}
        <Card UNSAFE_className="info-card">
          <div className="info-card-header">
            <span className="info-icon">📋</span>
            <Heading level={3} UNSAFE_className="info-title">Basic Information</Heading>
          </div>
          <div className="info-rows">
            <InfoRow icon="🎂" label="Date of Birth" value={formatDate(data.dateOfBirth)} />
            <InfoRow icon="🚪" label="Room" value={data.roomNumber || 'Unassigned'} />
            <InfoRow icon="📅" label="Admitted" value={formatDate(data.admissionDate)} />
            <InfoRow icon="🔖" label="MRN" value={`MRN-${String(data.id).padStart(6, '0')}`} />
          </div>
        </Card>

        {/* Address Card */}
        {data.address && (
          <Card UNSAFE_className="info-card">
            <div className="info-card-header">
              <span className="info-icon">📍</span>
              <Heading level={3} UNSAFE_className="info-title">Address</Heading>
            </div>
            <div className="address-block">
              <span className="address-street">{data.address.street}</span>
              <span className="address-city">{data.address.city}, {data.address.state} {data.address.zipCode}</span>
            </div>
          </Card>
        )}

        {/* Emergency Contacts */}
        <Card UNSAFE_className="info-card contacts-card">
          <div className="info-card-header">
            <span className="info-icon">📞</span>
            <Heading level={3} UNSAFE_className="info-title">Emergency Contacts</Heading>
            <Badge variant="neutral" size="S">{data.emergencyContacts?.length || 0}</Badge>
          </div>
          {data.emergencyContacts && data.emergencyContacts.length > 0 ? (
            <div className="contacts-list">
              {data.emergencyContacts.map((contact, i) => (
                <ContactCard key={i} contact={contact} isPrimary={i === 0} />
              ))}
            </div>
          ) : (
            <div className="empty-contacts">No emergency contacts on file.</div>
          )}
        </Card>

        {/* Quick Actions */}
        <Card UNSAFE_className="info-card actions-card">
          <div className="info-card-header">
            <span className="info-icon">⚡</span>
            <Heading level={3} UNSAFE_className="info-title">Quick Actions</Heading>
          </div>
          <div className="action-buttons">
            <Link to={listMedicationsPath(data.id)} className="action-button">
              <span className="action-icon">💊</span>
              <span>Medications</span>
            </Link>
            <Link to={getCarePlanPath(data.id)} className="action-button">
              <span className="action-icon">📋</span>
              <span>Care Plan</span>
            </Link>
            <button className="action-button">
              <span className="action-icon">📝</span>
              <span>Add Note</span>
            </button>
            <button className="action-button">
              <span className="action-icon">👨‍👩‍👧</span>
              <span>Contact Family</span>
            </button>
          </div>
        </Card>
      </div>
    </div>
  );
}

// Info Row Component
function InfoRow({ icon, label, value }: { icon: string; label: string; value: string }) {
  return (
    <div className="info-row">
      <span className="info-row-icon">{icon}</span>
      <span className="info-row-label">{label}</span>
      <span className="info-row-value">{value}</span>
    </div>
  );
}

// Contact Card
function ContactCard({ contact, isPrimary }: { contact: EmergencyContact; isPrimary: boolean }) {
  return (
    <div className={`contact-card ${isPrimary ? 'contact-primary' : ''}`}>
      <div className="contact-header">
        <span className="contact-name">{contact.name}</span>
        <Badge variant={isPrimary ? 'positive' : 'neutral'} size="S">
          {isPrimary ? 'Primary' : contact.relationship}
        </Badge>
      </div>
      <div className="contact-details">
        <a href={`tel:${contact.phone}`} className="contact-phone">{contact.phone}</a>
        {contact.email && (
          <a href={`mailto:${contact.email}`} className="contact-email">{contact.email}</a>
        )}
      </div>
    </div>
  );
}

// Utilities
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
    month: 'short',
    day: 'numeric',
  });
}
