import React, { useState } from 'react';
import { useNavigate } from '@tanstack/react-router';
import {
  Card,
  Button,
  ButtonGroup,
  Heading,
  Text,
  TextField,
  Divider,
  Checkbox,
} from '@react-spectrum/s2';
import type { CreateResidentRequest, Address, EmergencyContact } from '../../generated/types';
import { CreateResidentRequestSchema } from '../../generated/validation';
import { useCreateResidentMutation } from '../../generated/mutations';
import { useImpulse } from '../../client/shared/ImpulseProvider';
import { ZodError } from 'zod';

// ========================================
// Create Resident Form
// Demonstrates:
// - Generated mutation hooks
// - Zod validation from generated schemas
// - S2 form components
// - Server-driven mutations (Impulse way)
// ========================================

interface CreateResidentFormProps {
  onCancel: () => void;
  onSuccess?: (id: number) => void;
}

export function CreateResidentForm({ onCancel, onSuccess }: CreateResidentFormProps) {
  const ctx = useImpulse();
  const navigate = useNavigate();
  const mutation = useCreateResidentMutation(ctx);

  // Form state
  const [formData, setFormData] = useState<Partial<CreateResidentRequest>>({
    firstName: '',
    lastName: '',
    dateOfBirth: '',
    roomNumber: '',
    address: undefined,
    emergencyContacts: [],
  });

  const [includeAddress, setIncludeAddress] = useState(false);
  const [emergencyContacts, setEmergencyContacts] = useState<EmergencyContact[]>([]);
  const [errors, setErrors] = useState<Record<string, string>>({});

  // Handle form field changes
  const handleChange = (field: keyof CreateResidentRequest, value: string) => {
    setFormData(prev => ({ ...prev, [field]: value }));
    // Clear error when user types
    if (errors[field]) {
      setErrors(prev => ({ ...prev, [field]: '' }));
    }
  };

  // Handle address changes
  const handleAddressChange = (field: keyof Address, value: string) => {
    setFormData(prev => ({
      ...prev,
      address: {
        street: prev.address?.street || '',
        city: prev.address?.city || '',
        state: prev.address?.state || '',
        zipCode: prev.address?.zipCode || '',
        [field]: value,
      },
    }));
  };

  // Add emergency contact
  const addEmergencyContact = () => {
    setEmergencyContacts(prev => [
      ...prev,
      { name: '', relationship: '', phone: '', email: '' },
    ]);
  };

  // Update emergency contact
  const updateEmergencyContact = (index: number, field: keyof EmergencyContact, value: string) => {
    setEmergencyContacts(prev => prev.map((contact, i) =>
      i === index ? { ...contact, [field]: value } : contact
    ));
  };

  // Remove emergency contact
  const removeEmergencyContact = (index: number) => {
    setEmergencyContacts(prev => prev.filter((_, i) => i !== index));
  };

  // Handle form submission
  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    setErrors({});

    // Prepare request data
    const request: CreateResidentRequest = {
      firstName: formData.firstName || '',
      lastName: formData.lastName || '',
      dateOfBirth: formData.dateOfBirth || '',
      roomNumber: formData.roomNumber || '',
      address: includeAddress ? formData.address : undefined,
      emergencyContacts: emergencyContacts.length > 0 ? emergencyContacts : undefined,
    };

    try {
      // Validate with Zod (from generated schemas)
      CreateResidentRequestSchema.parse(request);

      // Execute mutation
      const result = await mutation.mutateAsync(request);

      // Success - navigate to the new resident
      if (onSuccess) {
        onSuccess(result.id);
      } else {
        navigate({ to: `/residents/${result.id}` });
      }
    } catch (err) {
      if (err instanceof ZodError) {
        // Convert Zod errors to field errors
        const fieldErrors: Record<string, string> = {};
        err.errors.forEach(error => {
          const path = error.path.join('.');
          fieldErrors[path] = error.message;
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
        <Heading level={2}>Create New Resident</Heading>
        <Text UNSAFE_className="impulse-muted impulse-mb-4">
          Enter the resident's information below. Fields marked with * are required.
        </Text>
        <Divider />

        {errors.form && (
          <div className="form-error impulse-mb-4">
            <Text UNSAFE_className="impulse-refused">{errors.form}</Text>
          </div>
        )}

        {/* Basic Information */}
        <div className="form-section">
          <Heading level={3}>Basic Information</Heading>

          <div className="form-row">
            <div className="impulse-flex impulse-flex-col impulse-gap-1">
              <TextField
                label="First Name *"
                value={formData.firstName || ''}
                onChange={(value) => handleChange('firstName', value)}
                isRequired
                validationState={errors.firstName ? 'invalid' : undefined}
              />
              {errors.firstName && <Text UNSAFE_className="impulse-refused impulse-muted-sm">{errors.firstName}</Text>}
            </div>
            <div className="impulse-flex impulse-flex-col impulse-gap-1">
              <TextField
                label="Last Name *"
                value={formData.lastName || ''}
                onChange={(value) => handleChange('lastName', value)}
                isRequired
                validationState={errors.lastName ? 'invalid' : undefined}
              />
              {errors.lastName && <Text UNSAFE_className="impulse-refused impulse-muted-sm">{errors.lastName}</Text>}
            </div>
          </div>

          <div className="form-row">
            <div className="impulse-flex impulse-flex-col impulse-gap-1">
              <TextField
                label="Date of Birth * (YYYY-MM-DD)"
                value={formData.dateOfBirth || ''}
                onChange={(value) => handleChange('dateOfBirth', value)}
                isRequired
                validationState={errors.dateOfBirth ? 'invalid' : undefined}
              />
              {errors.dateOfBirth && <Text UNSAFE_className="impulse-refused impulse-muted-sm">{errors.dateOfBirth}</Text>}
            </div>
            <div className="impulse-flex impulse-flex-col impulse-gap-1">
              <TextField
                label="Room Number *"
                value={formData.roomNumber || ''}
                onChange={(value) => handleChange('roomNumber', value)}
                isRequired
                validationState={errors.roomNumber ? 'invalid' : undefined}
              />
              {errors.roomNumber && <Text UNSAFE_className="impulse-refused impulse-muted-sm">{errors.roomNumber}</Text>}
            </div>
          </div>
        </div>

        <Divider />

        {/* Address Section */}
        <div className="form-section">
          <div className="impulse-flex impulse-items-center impulse-gap-4">
            <Heading level={3} UNSAFE_style={{ margin: 0 }}>Address</Heading>
            <Checkbox
              isSelected={includeAddress}
              onChange={setIncludeAddress}
            >
              Include address
            </Checkbox>
          </div>

          {includeAddress && (
            <div className="impulse-mt-4">
              <TextField
                label="Street"
                value={formData.address?.street || ''}
                onChange={(value) => handleAddressChange('street', value)}
              />
              <div className="form-row impulse-mt-4">
                <TextField
                  label="City"
                  value={formData.address?.city || ''}
                  onChange={(value) => handleAddressChange('city', value)}
                />
                <TextField
                  label="State"
                  value={formData.address?.state || ''}
                  onChange={(value) => handleAddressChange('state', value)}
                />
                <TextField
                  label="ZIP Code"
                  value={formData.address?.zipCode || ''}
                  onChange={(value) => handleAddressChange('zipCode', value)}
                />
              </div>
            </div>
          )}
        </div>

        <Divider />

        {/* Emergency Contacts */}
        <div className="form-section">
          <div className="impulse-flex impulse-justify-between impulse-items-center">
            <Heading level={3} UNSAFE_style={{ margin: 0 }}>Emergency Contacts</Heading>
            <Button variant="secondary" onPress={addEmergencyContact}>
              + Add Contact
            </Button>
          </div>

          {emergencyContacts.map((contact, index) => (
            <Card key={index} UNSAFE_className="impulse-contact-card impulse-mt-4">
              <div className="impulse-flex impulse-justify-between impulse-items-start impulse-mb-4">
                <Text UNSAFE_className="impulse-value">Contact {index + 1}</Text>
                <Button
                  variant="secondary"
                  size="S"
                  onPress={() => removeEmergencyContact(index)}
                >
                  Remove
                </Button>
              </div>
              <div className="form-row">
                <TextField
                  label="Name"
                  value={contact.name}
                  onChange={(value) => updateEmergencyContact(index, 'name', value)}
                />
                <TextField
                  label="Relationship"
                  value={contact.relationship}
                  onChange={(value) => updateEmergencyContact(index, 'relationship', value)}
                />
              </div>
              <div className="form-row impulse-mt-4">
                <TextField
                  label="Phone"
                  value={contact.phone}
                  onChange={(value) => updateEmergencyContact(index, 'phone', value)}
                />
                <TextField
                  label="Email"
                  value={contact.email}
                  onChange={(value) => updateEmergencyContact(index, 'email', value)}
                />
              </div>
            </Card>
          ))}

          {emergencyContacts.length === 0 && (
            <Text UNSAFE_className="impulse-muted impulse-mt-4">
              No emergency contacts added yet.
            </Text>
          )}
        </div>

        <Divider />

        {/* Form Actions */}
        <div className="form-actions">
          <ButtonGroup>
            <Button variant="secondary" onPress={onCancel}>
              Cancel
            </Button>
            <Button
              type="submit"
              variant="accent"
              isPending={mutation.isPending}
            >
              {mutation.isPending ? 'Creating...' : 'Create Resident'}
            </Button>
          </ButtonGroup>
        </div>
      </form>
    </Card>
  );
}

// ========================================
// Create Resident Page
// Standalone page wrapper
// ========================================

export function CreateResidentPage() {
  const navigate = useNavigate();

  return (
    <div className="form-container">
      <header className="form-header">
        <Heading level={1} UNSAFE_className="form-title">New Resident</Heading>
        <Text UNSAFE_className="form-subtitle">
          Create a new resident profile for your facility
        </Text>
      </header>
      <CreateResidentForm
        onCancel={() => navigate({ to: '/residents' })}
        onSuccess={(id) => navigate({ to: `/residents/${id}` })}
      />
    </div>
  );
}
