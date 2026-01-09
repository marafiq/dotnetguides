import React, { useState } from 'react';
import {
  Button,
  ButtonGroup,
  Heading,
  Text,
  TextField,
  Checkbox,
  Divider,
  Badge,
  Card,
} from '@react-spectrum/s2';
import type {
  RecordAdministrationRequest,
  Dosage,
  GetMedicationResponse,
} from '../../generated/types';
import { RecordAdministrationRequestSchema } from '../../generated/validation';
import { useRecordAdministrationMutation } from '../../generated/mutations';
import { useImpulse } from '../../client/shared/ImpulseProvider';
import { formatDosage } from '../../client/shared/utils';
import { ZodError } from 'zod';

// ========================================
// Administer Medication Modal
// Demonstrates:
// - Complex mutation with nested data (Dosage)
// - Modal dialog pattern
// - Zod validation
// - Real-time form state
// ========================================

interface AdministerModalProps {
  medication: GetMedicationResponse;
  onClose: () => void;
  onSuccess?: () => void;
}

export function AdministerMedicationForm({ medication, onClose, onSuccess }: AdministerModalProps) {
  const ctx = useImpulse();
  const mutation = useRecordAdministrationMutation(ctx);

  const [wasRefused, setWasRefused] = useState(false);
  const [errors, setErrors] = useState<Record<string, string>>({});

  // Form state
  const [formData, setFormData] = useState({
    administeredAt: new Date().toISOString().slice(0, 16),
    dosageAmount: medication.prescribedDosage?.amount || 0,
    dosageUnit: medication.prescribedDosage?.unit || '',
    specialInstructions: medication.prescribedDosage?.specialInstructions || '',
    notes: '',
    refusalReason: '',
  });

  const handleChange = (field: string, value: string | number) => {
    setFormData(prev => ({ ...prev, [field]: value }));
    if (errors[field]) {
      setErrors(prev => ({ ...prev, [field]: '' }));
    }
  };

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    setErrors({});

    const dosageGiven: Dosage | undefined = wasRefused ? undefined : {
      amount: formData.dosageAmount,
      unit: formData.dosageUnit,
      specialInstructions: formData.specialInstructions,
    };

    const request: RecordAdministrationRequest = {
      residentId: medication.residentId,
      medicationId: medication.id,
      administeredAt: new Date(formData.administeredAt).toISOString(),
      dosageGiven,
      notes: formData.notes,
      wasRefused,
      refusalReason: wasRefused ? formData.refusalReason : '',
    };

    try {
      RecordAdministrationRequestSchema.parse(request);
      await mutation.mutateAsync(request);
      onClose();
      onSuccess?.();
    } catch (err) {
      if (err instanceof ZodError) {
        const fieldErrors: Record<string, string> = {};
        err.errors.forEach(error => {
          fieldErrors[error.path.join('.')] = error.message;
        });
        setErrors(fieldErrors);
      } else if (err instanceof Error) {
        setErrors({ form: err.message });
      }
    }
  };

  return (
    <Card UNSAFE_className="impulse-card form-card">
      <form onSubmit={handleSubmit}>
        <Heading level={2}>Administer Medication</Heading>
        <Divider />

        {/* Medication Info */}
        <Card UNSAFE_className="impulse-info-card impulse-mb-4 impulse-mt-4">
          <div className="impulse-flex impulse-justify-between impulse-items-center">
            <div>
              <Text UNSAFE_className="impulse-value">{medication.drugName}</Text>
              {medication.genericName && medication.genericName !== medication.drugName && (
                <Text UNSAFE_className="impulse-muted-sm"> ({medication.genericName})</Text>
              )}
            </div>
            <Badge variant="informative" size="S">{medication.route}</Badge>
          </div>
          <div className="impulse-mt-2">
            <Text UNSAFE_className="impulse-label">Prescribed Dosage: </Text>
            <Text>{formatDosage(medication.prescribedDosage)}</Text>
          </div>
        </Card>

        {errors.form && (
          <div className="form-error impulse-mb-4">
            <Text UNSAFE_className="impulse-refused">{errors.form}</Text>
          </div>
        )}

        {/* Refused Checkbox */}
        <div className="impulse-mb-4">
          <Checkbox
            isSelected={wasRefused}
            onChange={setWasRefused}
          >
            Resident refused medication
          </Checkbox>
        </div>

        <Divider />

        {!wasRefused ? (
          <div className="form-section">
            <Heading level={3}>Administration Details</Heading>

            <TextField
              label="Administration Time"
              value={formData.administeredAt}
              onChange={(value) => handleChange('administeredAt', value)}
              isRequired
            />

            <div className="form-row impulse-mt-4">
              <TextField
                label="Dosage Amount"
                value={String(formData.dosageAmount)}
                onChange={(value) => handleChange('dosageAmount', parseFloat(value) || 0)}
                isRequired
              />
              <TextField
                label="Unit"
                value={formData.dosageUnit}
                onChange={(value) => handleChange('dosageUnit', value)}
                isRequired
              />
            </div>

            <div className="impulse-mt-4">
              <TextField
                label="Special Instructions"
                value={formData.specialInstructions}
                onChange={(value) => handleChange('specialInstructions', value)}
              />
            </div>

            <div className="impulse-mt-4">
              <TextField
                label="Notes"
                value={formData.notes}
                onChange={(value) => handleChange('notes', value)}
                description="Any observations or additional notes"
              />
            </div>
          </div>
        ) : (
          <div className="form-section">
            <Heading level={3}>Refusal Details</Heading>

            <TextField
              label="Time of Refusal"
              value={formData.administeredAt}
              onChange={(value) => handleChange('administeredAt', value)}
              isRequired
            />

            <div className="impulse-mt-4">
              <TextField
                label="Reason for Refusal *"
                value={formData.refusalReason}
                onChange={(value) => handleChange('refusalReason', value)}
                isRequired
                validationState={errors.refusalReason ? 'invalid' : undefined}
                description="Document why the resident refused the medication"
              />
              {errors.refusalReason && (
                <Text UNSAFE_className="impulse-refused impulse-muted-sm">{errors.refusalReason}</Text>
              )}
            </div>

            <div className="impulse-mt-4">
              <TextField
                label="Additional Notes"
                value={formData.notes}
                onChange={(value) => handleChange('notes', value)}
                description="Any follow-up actions or observations"
              />
            </div>
          </div>
        )}

        <Divider />

        {/* Actions */}
        <div className="form-actions">
          <ButtonGroup>
            <Button variant="secondary" onPress={onClose}>
              Cancel
            </Button>
            <Button
              type="submit"
              variant={wasRefused ? 'negative' : 'accent'}
              isPending={mutation.isPending}
            >
              {mutation.isPending
                ? 'Recording...'
                : wasRefused
                  ? 'Record Refusal'
                  : 'Record Administration'
              }
            </Button>
          </ButtonGroup>
        </div>
      </form>
    </Card>
  );
}

// ========================================
// Quick Administer Button with Modal
// ========================================

interface QuickAdministerProps {
  medication: GetMedicationResponse;
  onSuccess?: () => void;
}

export function QuickAdministerButton({ medication, onSuccess }: QuickAdministerProps) {
  const [isOpen, setIsOpen] = useState(false);

  if (isOpen) {
    return (
      <div className="modal-overlay">
        <div className="modal-content">
          <AdministerMedicationForm
            medication={medication}
            onClose={() => setIsOpen(false)}
            onSuccess={onSuccess}
          />
        </div>
      </div>
    );
  }

  return (
    <Button variant="accent" onPress={() => setIsOpen(true)}>
      Administer Now
    </Button>
  );
}
