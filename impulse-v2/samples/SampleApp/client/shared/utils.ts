/**
 * Shared Utilities for Impulse Components
 * Centralizes common logic to avoid duplication
 */

import type { MedicationStatus, CareGoalStatus } from '../../generated/types';

// ========================================
// Status Variant Mapping
// ========================================

export type StatusVariant = 'positive' | 'negative' | 'notice' | 'informative' | 'neutral';

export function getResidentStatusVariant(status: string): StatusVariant {
  switch (status) {
    case 'Active': return 'positive';
    case 'Hospitalized': return 'negative';
    case 'OnLeave': return 'notice';
    case 'Discharged': return 'neutral';
    default: return 'informative';
  }
}

export function getMedicationStatusVariant(status: MedicationStatus): StatusVariant {
  switch (status) {
    case 'Active': return 'positive';
    case 'Discontinued': return 'negative';
    case 'OnHold': return 'notice';
    case 'Completed': return 'neutral';
    default: return 'informative';
  }
}

export function getCareGoalStatusVariant(status: CareGoalStatus): StatusVariant {
  switch (status) {
    case 'Active': return 'positive';
    case 'Completed': return 'informative';
    case 'OnHold': return 'notice';
    case 'Cancelled': return 'negative';
    default: return 'neutral';
  }
}

// ========================================
// Date/Time Formatting
// ========================================

export function formatDate(dateString: string): string {
  return new Date(dateString).toLocaleDateString('en-US', {
    year: 'numeric',
    month: 'long',
    day: 'numeric',
  });
}

export function formatShortDate(dateString: string): string {
  return new Date(dateString).toLocaleDateString('en-US', {
    year: 'numeric',
    month: 'short',
    day: 'numeric',
  });
}

export function formatDateTime(dateString: string): string {
  return new Date(dateString).toLocaleString('en-US', {
    month: 'short',
    day: 'numeric',
    hour: 'numeric',
    minute: '2-digit',
    hour12: true,
  });
}

export function formatTime(dateString: string): string {
  return new Date(dateString).toLocaleTimeString('en-US', {
    hour: 'numeric',
    minute: '2-digit',
    hour12: true,
  });
}

export function formatRelativeTime(dateString: string): string {
  const date = new Date(dateString);
  const now = new Date();
  const diffMs = now.getTime() - date.getTime();
  const diffDays = Math.floor(diffMs / (1000 * 60 * 60 * 24));

  if (diffDays === 0) return 'Today';
  if (diffDays === 1) return 'Yesterday';
  if (diffDays < 7) return `${diffDays} days ago`;
  if (diffDays < 30) return `${Math.floor(diffDays / 7)} weeks ago`;
  return formatShortDate(dateString);
}

// ========================================
// Age Calculation
// ========================================

export function calculateAge(dateOfBirth: string): number {
  const dob = new Date(dateOfBirth);
  const today = new Date();
  let age = today.getFullYear() - dob.getFullYear();
  const monthDiff = today.getMonth() - dob.getMonth();
  if (monthDiff < 0 || (monthDiff === 0 && today.getDate() < dob.getDate())) {
    age--;
  }
  return age;
}

// ========================================
// Dosage Formatting
// ========================================

import type { Dosage } from '../../generated/types';

export function formatDosage(dosage?: Dosage): string {
  if (!dosage) return '-';
  let result = `${dosage.amount} ${dosage.unit}`;
  if (dosage.specialInstructions) {
    result += ` - ${dosage.specialInstructions}`;
  }
  return result;
}

// ========================================
// Medical Record Number
// ========================================

export function formatMRN(id: number): string {
  return `MRN-${String(id).padStart(6, '0')}`;
}

// ========================================
// Percent/Progress Calculations
// ========================================

export function calculateProgress(completed: number, total: number): number {
  if (total === 0) return 0;
  return Math.round((completed / total) * 100);
}
