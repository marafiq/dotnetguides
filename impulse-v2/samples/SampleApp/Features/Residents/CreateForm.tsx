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
  Form,
} from '@react-spectrum/s2';
import type { CreateResidentRequest, Address, EmergencyContact } from '../../generated/types';
import { useCreateResidentMutation } from '../../generated/mutations';
import { useImpulse } from '../../client/shared/ImpulseProvider';
import { ImpulseValidationError } from '../../client/impulse-runtime';
import { z, ZodError } from 'zod';

// ========================================
// Form Validation Schema (Impulse Way)
// Demonstrates nested Zod validation with
// required fields and custom error messages
// ========================================

const AddressFormSchema = z.object({
  street: z.string().min(1, 'Street is required'),
  city: z.string().min(1, 'City is required'),
  state: z.string().min(1, 'State is required'),
  zipCode: z.string().min(1, 'ZIP Code is required'),
});

const EmergencyContactFormSchema = z.object({
  name: z.string().min(1, 'Contact name is required'),
  relationship: z.string().min(1, 'Relationship is required'),
  phone: z.string().min(1, 'Phone number is required'),
  email: z.string().email('Valid email is required'),
});

const CreateResidentFormSchema = z.object({
  firstName: z.string().min(1, 'First name is required'),
  lastName: z.string().min(1, 'Last name is required'),
  dateOfBirth: z.string().datetime({ message: 'Valid date required (YYYY-MM-DDTHH:mm:ssZ)' }),
  roomNumber: z.string().min(1, 'Room number is required'),
  address: AddressFormSchema.optional(),
  emergencyContacts: z.array(EmergencyContactFormSchema).optional(),
});

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
      // Validate with Zod (Impulse way - nested validation)
      CreateResidentFormSchema.parse(request);

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
        // Client-side validation (Zod) - Impulse way
        const fieldErrors: Record<string, string> = {};
        err.errors.forEach(error => {
          const path = error.path.join('.');
          fieldErrors[path] = error.message;
        });
        setErrors(fieldErrors);
      } else if (err instanceof ImpulseValidationError) {
        // Server-side validation (ProblemDetails) - Impulse way
        // The server returned RFC 7807 ProblemDetails with field errors
        setErrors(err.fieldErrors);
      } else if (err instanceof Error) {
        setErrors({ form: err.message });
      }
    }
  };

  return (
    <Card UNSAFE_className="impulse-card form-card">
      {/* S2 Form component with validationErrors - the Impulse way
          Errors from both Zod (client) and ProblemDetails (server)
          are displayed automatically by S2 using field names */}
      <Form
        onSubmit={handleSubmit}
        validationErrors={errors}
        validationBehavior="aria"
      >
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
            <TextField
              label="First Name *"
              name="firstName"
              value={formData.firstName || ''}
              onChange={(value) => handleChange('firstName', value)}
              isRequired
            />
            <TextField
              label="Last Name *"
              name="lastName"
              value={formData.lastName || ''}
              onChange={(value) => handleChange('lastName', value)}
              isRequired
            />
          </div>

          <div className="form-row">
            <TextField
              label="Date of Birth * (YYYY-MM-DD)"
              name="dateOfBirth"
              value={formData.dateOfBirth || ''}
              onChange={(value) => handleChange('dateOfBirth', value)}
              isRequired
            />
            <TextField
              label="Room Number *"
              name="roomNumber"
              value={formData.roomNumber || ''}
              onChange={(value) => handleChange('roomNumber', value)}
              isRequired
            />
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
                label="Street *"
                name="address.street"
                value={formData.address?.street || ''}
                onChange={(value) => handleAddressChange('street', value)}
                isRequired
              />
              <div className="form-row impulse-mt-4">
                <TextField
                  label="City *"
                  name="address.city"
                  value={formData.address?.city || ''}
                  onChange={(value) => handleAddressChange('city', value)}
                  isRequired
                />
                <TextField
                  label="State *"
                  name="address.state"
                  value={formData.address?.state || ''}
                  onChange={(value) => handleAddressChange('state', value)}
                  isRequired
                />
                <TextField
                  label="ZIP Code *"
                  name="address.zipCode"
                  value={formData.address?.zipCode || ''}
                  onChange={(value) => handleAddressChange('zipCode', value)}
                  isRequired
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
                  label="Name *"
                  name={`emergencyContacts.${index}.name`}
                  value={contact.name}
                  onChange={(value) => updateEmergencyContact(index, 'name', value)}
                  isRequired
                />
                <TextField
                  label="Relationship *"
                  name={`emergencyContacts.${index}.relationship`}
                  value={contact.relationship}
                  onChange={(value) => updateEmergencyContact(index, 'relationship', value)}
                  isRequired
                />
              </div>
              <div className="form-row impulse-mt-4">
                <TextField
                  label="Phone *"
                  name={`emergencyContacts.${index}.phone`}
                  value={contact.phone}
                  onChange={(value) => updateEmergencyContact(index, 'phone', value)}
                  isRequired
                />
                <TextField
                  label="Email *"
                  name={`emergencyContacts.${index}.email`}
                  value={contact.email}
                  onChange={(value) => updateEmergencyContact(index, 'email', value)}
                  isRequired
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
      </Form>
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
