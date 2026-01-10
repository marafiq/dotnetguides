import { useState, useEffect, useCallback } from 'react';
import { useNavigate } from '@tanstack/react-router';
import {
  Card,
  Button,
  Heading,
  Text,
  TextField,
  Checkbox,
  Divider,
  Form,
  ProgressBar,
  Badge,
  ActionButton,
} from '@react-spectrum/s2';
import { z, ZodError } from 'zod';
import { useImpulse } from '../../client/shared/ImpulseProvider';
import { ImpulseValidationError } from '../../client/impulse-runtime';

// ========================================
// Impulse Admission Wizard - The Impulse Way
// Demonstrates:
// - Server-driven wizard configuration
// - Step-by-step Zod validation (client)
// - Step-by-step ProblemDetails validation (server)
// - S2 Form with validationErrors per step
// - Progress tracking and navigation
// ========================================

// ========================================
// Types - Match server DTOs
// ========================================

interface WizardStepConfig {
  stepNumber: number;
  id: string;
  title: string;
  description: string;
  isRequired: boolean;
  isComplete: boolean;
  fields: WizardFieldConfig[];
}

interface WizardFieldConfig {
  name: string;
  label: string;
  type: string;
  isRequired: boolean;
  placeholder?: string;
  helpText?: string;
  options?: string[];
}

interface WizardConfig {
  wizardId: string;
  title: string;
  description: string;
  steps: WizardStepConfig[];
  currentStep: number;
  totalSteps: number;
}

interface EmergencyContact {
  name: string;
  relationship: string;
  phone: string;
  email: string;
  isPrimaryContact: boolean;
}

// ========================================
// Zod Schemas - Client-side validation per step
// ========================================

const BasicInfoSchema = z.object({
  firstName: z.string().min(2, 'First name must be at least 2 characters'),
  lastName: z.string().min(2, 'Last name must be at least 2 characters'),
  dateOfBirth: z.string().min(1, 'Date of birth is required'),
  admissionDate: z.string().min(1, 'Admission date is required'),
  roomPreference: z.string().optional(),
});

const MedicalHistorySchema = z.object({
  existingConditions: z.array(z.string()).optional(),
  allergies: z.array(z.string()).optional(),
  currentMedications: z.array(z.string()).optional(),
  primaryCarePhysician: z.string().optional(),
  physicianPhone: z.string().optional(),
  specialInstructions: z.string().optional(),
});

const CarePreferencesSchema = z.object({
  dietaryRestrictions: z.string().optional(),
  mobilityLevel: z.string().min(1, 'Mobility level is required'),
  communicationPreference: z.string().min(1, 'Communication preference is required'),
  prefersMorningCare: z.boolean().optional(),
  prefersEveningCare: z.boolean().optional(),
  requiresPrivateRoom: z.boolean().optional(),
  additionalNotes: z.string().optional(),
});

const EmergencyContactSchema = z.object({
  name: z.string().min(1, 'Contact name is required'),
  relationship: z.string().min(1, 'Relationship is required'),
  phone: z.string().min(1, 'Phone number is required'),
  email: z.string().email('Valid email required').or(z.string().length(0)).optional(),
  isPrimaryContact: z.boolean(),
});

const EmergencyContactsSchema = z.object({
  contacts: z.array(EmergencyContactSchema).min(1, 'At least one emergency contact is required'),
});

const ReviewSchema = z.object({
  acceptsTerms: z.literal(true, { errorMap: () => ({ message: 'You must accept the terms' }) }),
  authorizesRelease: z.literal(true, { errorMap: () => ({ message: 'Authorization is required' }) }),
  additionalComments: z.string().optional(),
});

// Map step IDs to their schemas
const stepSchemas: Record<string, z.ZodSchema> = {
  'basic-info': BasicInfoSchema,
  'medical-history': MedicalHistorySchema,
  'care-preferences': CarePreferencesSchema,
  'emergency-contacts': EmergencyContactsSchema,
  'review': ReviewSchema,
};

// ========================================
// Wizard Component
// ========================================

export function AdmissionWizard() {
  const ctx = useImpulse();
  const navigate = useNavigate();

  // Wizard state
  const [config, setConfig] = useState<WizardConfig | null>(null);
  const [currentStep, setCurrentStep] = useState(1);
  const [errors, setErrors] = useState<Record<string, string>>({});
  const [isLoading, setIsLoading] = useState(true);
  const [isValidating, setIsValidating] = useState(false);
  const [completedSteps, setCompletedSteps] = useState<Set<number>>(new Set());

  // Form data for all steps
  const [formData, setFormData] = useState({
    // Step 1: Basic Info
    firstName: '',
    lastName: '',
    dateOfBirth: '',
    admissionDate: '',
    roomPreference: '',
    // Step 2: Medical History
    existingConditions: [] as string[],
    allergies: [] as string[],
    currentMedications: [] as string[],
    primaryCarePhysician: '',
    physicianPhone: '',
    specialInstructions: '',
    // Step 3: Care Preferences
    dietaryRestrictions: '',
    mobilityLevel: '',
    communicationPreference: '',
    prefersMorningCare: false,
    prefersEveningCare: false,
    requiresPrivateRoom: false,
    additionalNotes: '',
    // Step 4: Emergency Contacts
    contacts: [] as EmergencyContact[],
    // Step 5: Review
    acceptsTerms: false,
    authorizesRelease: false,
    additionalComments: '',
  });

  // Load wizard configuration from server
  useEffect(() => {
    async function loadConfig() {
      try {
        const response = await ctx.impulse<WizardConfig>('/admission/wizard');
        setConfig(response);
      } catch (err) {
        console.error('Failed to load wizard config:', err);
      } finally {
        setIsLoading(false);
      }
    }
    loadConfig();
  }, [ctx]);

  // Handle field changes
  const handleChange = useCallback((field: string, value: unknown) => {
    setFormData(prev => ({ ...prev, [field]: value }));
    // Clear error when user types
    if (errors[field]) {
      setErrors(prev => {
        const next = { ...prev };
        delete next[field];
        return next;
      });
    }
  }, [errors]);

  // Get current step data for validation
  const getStepData = useCallback((stepId: string) => {
    switch (stepId) {
      case 'basic-info':
        return {
          firstName: formData.firstName,
          lastName: formData.lastName,
          dateOfBirth: formData.dateOfBirth,
          admissionDate: formData.admissionDate,
          roomPreference: formData.roomPreference,
        };
      case 'medical-history':
        return {
          existingConditions: formData.existingConditions,
          allergies: formData.allergies,
          currentMedications: formData.currentMedications,
          primaryCarePhysician: formData.primaryCarePhysician,
          physicianPhone: formData.physicianPhone,
          specialInstructions: formData.specialInstructions,
        };
      case 'care-preferences':
        return {
          dietaryRestrictions: formData.dietaryRestrictions,
          mobilityLevel: formData.mobilityLevel,
          communicationPreference: formData.communicationPreference,
          prefersMorningCare: formData.prefersMorningCare,
          prefersEveningCare: formData.prefersEveningCare,
          requiresPrivateRoom: formData.requiresPrivateRoom,
          additionalNotes: formData.additionalNotes,
        };
      case 'emergency-contacts':
        return { contacts: formData.contacts };
      case 'review':
        return {
          acceptsTerms: formData.acceptsTerms,
          authorizesRelease: formData.authorizesRelease,
          additionalComments: formData.additionalComments,
        };
      default:
        return {};
    }
  }, [formData]);

  // Validate current step (Zod + Server)
  const validateStep = useCallback(async (stepId: string): Promise<boolean> => {
    setErrors({});
    setIsValidating(true);

    const stepData = getStepData(stepId);
    const schema = stepSchemas[stepId];

    try {
      // Step 1: Client-side Zod validation
      if (schema) {
        schema.parse(stepData);
      }

      // Step 2: Server-side validation (Impulse way - ProblemDetails)
      await ctx.impulseMutate(`/admission/wizard/validate/${stepId}`, stepData, 'POST');

      return true;
    } catch (err) {
      if (err instanceof ZodError) {
        // Client validation errors
        const fieldErrors: Record<string, string> = {};
        err.errors.forEach(error => {
          const path = error.path.join('.');
          fieldErrors[path] = error.message;
        });
        setErrors(fieldErrors);
      } else if (err instanceof ImpulseValidationError) {
        // Server validation errors (ProblemDetails)
        setErrors(err.fieldErrors);
      }
      return false;
    } finally {
      setIsValidating(false);
    }
  }, [ctx, getStepData]);

  // Navigate to next step
  const handleNext = useCallback(async () => {
    if (!config) return;

    const step = config.steps[currentStep - 1];
    const isValid = await validateStep(step.id);

    if (isValid) {
      setCompletedSteps(prev => new Set([...prev, currentStep]));
      if (currentStep < config.totalSteps) {
        setCurrentStep(prev => prev + 1);
        setErrors({});
      }
    }
  }, [config, currentStep, validateStep]);

  // Navigate to previous step
  const handleBack = useCallback(() => {
    if (currentStep > 1) {
      setCurrentStep(prev => prev - 1);
      setErrors({});
    }
  }, [currentStep]);

  // Submit wizard
  const handleSubmit = useCallback(async () => {
    if (!config) return;

    // Validate final step
    const step = config.steps[currentStep - 1];
    const isValid = await validateStep(step.id);
    if (!isValid) return;

    setIsValidating(true);
    try {
      // Submit complete admission
      const result = await ctx.impulseMutate<unknown, {
        residentId: number;
        confirmationNumber: string;
      }>('/admission/wizard/complete', {
        basicInfo: getStepData('basic-info'),
        medicalHistory: getStepData('medical-history'),
        carePreferences: getStepData('care-preferences'),
        emergencyContacts: getStepData('emergency-contacts'),
        acceptsTerms: formData.acceptsTerms,
        authorizesRelease: formData.authorizesRelease,
      }, 'POST');

      // Success - navigate to confirmation or resident
      navigate({ to: `/residents/${result.residentId}` });
    } catch (err) {
      if (err instanceof ImpulseValidationError) {
        setErrors(err.fieldErrors);
      } else if (err instanceof Error) {
        setErrors({ form: err.message });
      }
    } finally {
      setIsValidating(false);
    }
  }, [config, currentStep, validateStep, ctx, formData, getStepData, navigate]);

  // Add emergency contact
  const addContact = useCallback(() => {
    setFormData(prev => ({
      ...prev,
      contacts: [
        ...prev.contacts,
        { name: '', relationship: '', phone: '', email: '', isPrimaryContact: prev.contacts.length === 0 },
      ],
    }));
  }, []);

  // Update emergency contact
  const updateContact = useCallback((index: number, field: keyof EmergencyContact, value: unknown) => {
    setFormData(prev => ({
      ...prev,
      contacts: prev.contacts.map((c, i) =>
        i === index ? { ...c, [field]: value } : c
      ),
    }));
  }, []);

  // Remove emergency contact
  const removeContact = useCallback((index: number) => {
    setFormData(prev => ({
      ...prev,
      contacts: prev.contacts.filter((_, i) => i !== index),
    }));
  }, []);

  // Loading state
  if (isLoading || !config) {
    return (
      <div className="wizard-container">
        <Text>Loading wizard...</Text>
      </div>
    );
  }

  const step = config.steps[currentStep - 1];
  const progress = (currentStep / config.totalSteps) * 100;

  return (
    <div className="wizard-container">
      {/* Wizard Header */}
      <header className="wizard-header">
        <Heading level={1} UNSAFE_className="wizard-title">{config.title}</Heading>
        <Text UNSAFE_className="wizard-subtitle">{config.description}</Text>
      </header>

      {/* Progress Bar */}
      <div className="wizard-progress">
        <ProgressBar
          label={`Step ${currentStep} of ${config.totalSteps}`}
          value={progress}
          UNSAFE_className="wizard-progress-bar"
        />
        <div className="wizard-steps-nav">
          {config.steps.map((s, i) => (
            <div
              key={s.id}
              className={`wizard-step-indicator ${
                i + 1 === currentStep ? 'active' : ''
              } ${completedSteps.has(i + 1) ? 'completed' : ''}`}
            >
              <span className="step-number">{completedSteps.has(i + 1) ? '✓' : i + 1}</span>
              <span className="step-title">{s.title}</span>
            </div>
          ))}
        </div>
      </div>

      {/* Step Card */}
      <Card UNSAFE_className="wizard-card">
        <Form
          validationErrors={errors}
          validationBehavior="aria"
        >
          <div className="wizard-step-header">
            <Badge variant="informative" size="M">Step {currentStep}</Badge>
            <Heading level={2}>{step.title}</Heading>
            <Text UNSAFE_className="impulse-muted">{step.description}</Text>
          </div>

          <Divider />

          {/* Form Error */}
          {errors.form && (
            <div className="form-error">
              <Text UNSAFE_className="impulse-refused">{errors.form}</Text>
            </div>
          )}

          {/* Step Content */}
          <div className="wizard-step-content">
            {step.id === 'basic-info' && (
              <BasicInfoStep formData={formData} onChange={handleChange} />
            )}
            {step.id === 'medical-history' && (
              <MedicalHistoryStep formData={formData} onChange={handleChange} />
            )}
            {step.id === 'care-preferences' && (
              <CarePreferencesStep formData={formData} onChange={handleChange} />
            )}
            {step.id === 'emergency-contacts' && (
              <EmergencyContactsStep
                contacts={formData.contacts}
                onAdd={addContact}
                onUpdate={updateContact}
                onRemove={removeContact}
              />
            )}
            {step.id === 'review' && (
              <ReviewStep formData={formData} onChange={handleChange} />
            )}
          </div>

          <Divider />

          {/* Navigation */}
          <div className="wizard-actions">
            <Button
              variant="secondary"
              onPress={handleBack}
              isDisabled={currentStep === 1}
            >
              Back
            </Button>

            <div className="wizard-actions-right">
              {currentStep < config.totalSteps ? (
                <Button
                  variant="accent"
                  onPress={handleNext}
                  isPending={isValidating}
                >
                  {isValidating ? 'Validating...' : 'Next'}
                </Button>
              ) : (
                <Button
                  variant="accent"
                  onPress={handleSubmit}
                  isPending={isValidating}
                >
                  {isValidating ? 'Submitting...' : 'Complete Admission'}
                </Button>
              )}
            </div>
          </div>
        </Form>
      </Card>
    </div>
  );
}

// ========================================
// Step Components
// ========================================

interface StepProps {
  formData: Record<string, unknown>;
  onChange: (field: string, value: unknown) => void;
}

function BasicInfoStep({ formData, onChange }: StepProps) {
  return (
    <div className="wizard-fields">
      <div className="form-row">
        <TextField
          label="First Name *"
          name="firstName"
          value={formData.firstName as string}
          onChange={(v) => onChange('firstName', v)}
          isRequired
        />
        <TextField
          label="Last Name *"
          name="lastName"
          value={formData.lastName as string}
          onChange={(v) => onChange('lastName', v)}
          isRequired
        />
      </div>
      <div className="form-row">
        <TextField
          label="Date of Birth *"
          name="dateOfBirth"
          type="date"
          value={formData.dateOfBirth as string}
          onChange={(v) => onChange('dateOfBirth', v)}
          isRequired
          description="Resident must be at least 18 years old"
        />
        <TextField
          label="Admission Date *"
          name="admissionDate"
          type="date"
          value={formData.admissionDate as string}
          onChange={(v) => onChange('admissionDate', v)}
          isRequired
          description="When will the resident be admitted?"
        />
      </div>
      <TextField
        label="Room Preference"
        name="roomPreference"
        value={formData.roomPreference as string}
        onChange={(v) => onChange('roomPreference', v)}
        description="Private Room, Semi-Private, or No Preference"
      />
    </div>
  );
}

function MedicalHistoryStep({ formData, onChange }: StepProps) {
  const [allergyInput, setAllergyInput] = useState('');
  const [medInput, setMedInput] = useState('');

  const allergies = formData.allergies as string[];
  const medications = formData.currentMedications as string[];

  return (
    <div className="wizard-fields">
      <div className="tag-input-section">
        <Text UNSAFE_className="impulse-label-upper">Allergies</Text>
        <div className="tag-input-row">
          <TextField
            label="Add Allergy"
            value={allergyInput}
            onChange={setAllergyInput}
            onKeyDown={(e: React.KeyboardEvent) => {
              if (e.key === 'Enter' && allergyInput.trim()) {
                e.preventDefault();
                onChange('allergies', [...allergies, allergyInput.trim()]);
                setAllergyInput('');
              }
            }}
          />
          <Button
            variant="secondary"
            onPress={() => {
              if (allergyInput.trim()) {
                onChange('allergies', [...allergies, allergyInput.trim()]);
                setAllergyInput('');
              }
            }}
          >
            Add
          </Button>
        </div>
        <div className="tag-list">
          {allergies.map((a, i) => (
            <Badge key={i} variant="negative" size="M">
              {a}
              <ActionButton
                isQuiet
                onPress={() => onChange('allergies', allergies.filter((_, j) => j !== i))}
              >
                ×
              </ActionButton>
            </Badge>
          ))}
        </div>
      </div>

      <div className="tag-input-section">
        <Text UNSAFE_className="impulse-label-upper">Current Medications</Text>
        <div className="tag-input-row">
          <TextField
            label="Add Medication"
            value={medInput}
            onChange={setMedInput}
            onKeyDown={(e: React.KeyboardEvent) => {
              if (e.key === 'Enter' && medInput.trim()) {
                e.preventDefault();
                onChange('currentMedications', [...medications, medInput.trim()]);
                setMedInput('');
              }
            }}
          />
          <Button
            variant="secondary"
            onPress={() => {
              if (medInput.trim()) {
                onChange('currentMedications', [...medications, medInput.trim()]);
                setMedInput('');
              }
            }}
          >
            Add
          </Button>
        </div>
        <div className="tag-list">
          {medications.map((m, i) => (
            <Badge key={i} variant="informative" size="M">
              {m}
              <ActionButton
                isQuiet
                onPress={() => onChange('currentMedications', medications.filter((_, j) => j !== i))}
              >
                ×
              </ActionButton>
            </Badge>
          ))}
        </div>
      </div>

      <Divider />

      <div className="form-row">
        <TextField
          label="Primary Care Physician"
          name="primaryCarePhysician"
          value={formData.primaryCarePhysician as string}
          onChange={(v) => onChange('primaryCarePhysician', v)}
          description="Required if medications are listed"
        />
        <TextField
          label="Physician Phone"
          name="physicianPhone"
          value={formData.physicianPhone as string}
          onChange={(v) => onChange('physicianPhone', v)}
        />
      </div>

      <TextField
        label="Special Medical Instructions"
        name="specialInstructions"
        value={formData.specialInstructions as string}
        onChange={(v) => onChange('specialInstructions', v)}
        // Note: S2 TextField doesn't have multiline - would use TextArea
      />
    </div>
  );
}

function CarePreferencesStep({ formData, onChange }: StepProps) {
  return (
    <div className="wizard-fields">
      <TextField
        label="Dietary Restrictions"
        name="dietaryRestrictions"
        value={formData.dietaryRestrictions as string}
        onChange={(v) => onChange('dietaryRestrictions', v)}
        description="List any dietary requirements or restrictions"
      />

      <div className="form-row">
        <TextField
          label="Mobility Level *"
          name="mobilityLevel"
          value={formData.mobilityLevel as string}
          onChange={(v) => onChange('mobilityLevel', v)}
          isRequired
          description="Independent, Assistance Needed, Wheelchair, or Bedridden"
        />
        <TextField
          label="Communication Preference *"
          name="communicationPreference"
          value={formData.communicationPreference as string}
          onChange={(v) => onChange('communicationPreference', v)}
          isRequired
          description="Verbal, Written, Sign Language, or Interpreter Required"
        />
      </div>

      <div className="checkbox-group">
        <Checkbox
          name="prefersMorningCare"
          isSelected={formData.prefersMorningCare as boolean}
          onChange={(v) => onChange('prefersMorningCare', v)}
        >
          Prefers Morning Care
        </Checkbox>
        <Checkbox
          name="prefersEveningCare"
          isSelected={formData.prefersEveningCare as boolean}
          onChange={(v) => onChange('prefersEveningCare', v)}
        >
          Prefers Evening Care
        </Checkbox>
        <Checkbox
          name="requiresPrivateRoom"
          isSelected={formData.requiresPrivateRoom as boolean}
          onChange={(v) => onChange('requiresPrivateRoom', v)}
        >
          Requires Private Room
        </Checkbox>
      </div>

      <TextField
        label="Additional Notes"
        name="additionalNotes"
        value={formData.additionalNotes as string}
        onChange={(v) => onChange('additionalNotes', v)}
        description="Any other preferences or requirements"
      />
    </div>
  );
}

interface EmergencyContactsStepProps {
  contacts: EmergencyContact[];
  onAdd: () => void;
  onUpdate: (index: number, field: keyof EmergencyContact, value: unknown) => void;
  onRemove: (index: number) => void;
}

function EmergencyContactsStep({ contacts, onAdd, onUpdate, onRemove }: EmergencyContactsStepProps) {
  return (
    <div className="wizard-fields">
      <div className="contacts-header">
        <Text UNSAFE_className="impulse-label-upper">Emergency Contacts</Text>
        <Button variant="secondary" onPress={onAdd}>+ Add Contact</Button>
      </div>

      {contacts.length === 0 && (
        <div className="empty-contacts">
          <Text UNSAFE_className="impulse-muted">
            No emergency contacts added yet. At least one contact is required.
          </Text>
        </div>
      )}

      {contacts.map((contact, index) => (
        <Card key={index} UNSAFE_className="contact-card-wizard">
          <div className="contact-card-header">
            <Badge variant={contact.isPrimaryContact ? 'positive' : 'neutral'} size="M">
              {contact.isPrimaryContact ? 'Primary Contact' : `Contact ${index + 1}`}
            </Badge>
            <Button variant="secondary" size="S" onPress={() => onRemove(index)}>Remove</Button>
          </div>

          <div className="form-row">
            <TextField
              label="Name *"
              name={`contacts.${index}.name`}
              value={contact.name}
              onChange={(v) => onUpdate(index, 'name', v)}
              isRequired
            />
            <TextField
              label="Relationship *"
              name={`contacts.${index}.relationship`}
              value={contact.relationship}
              onChange={(v) => onUpdate(index, 'relationship', v)}
              isRequired
            />
          </div>

          <div className="form-row">
            <TextField
              label="Phone *"
              name={`contacts.${index}.phone`}
              value={contact.phone}
              onChange={(v) => onUpdate(index, 'phone', v)}
              isRequired
            />
            <TextField
              label="Email"
              name={`contacts.${index}.email`}
              value={contact.email}
              onChange={(v) => onUpdate(index, 'email', v)}
            />
          </div>

          <Checkbox
            name={`contacts.${index}.isPrimaryContact`}
            isSelected={contact.isPrimaryContact}
            onChange={(v) => {
              // Only one primary contact allowed
              contacts.forEach((_, i) => {
                if (i !== index) onUpdate(i, 'isPrimaryContact', false);
              });
              onUpdate(index, 'isPrimaryContact', v);
            }}
          >
            Primary Contact
          </Checkbox>
        </Card>
      ))}
    </div>
  );
}

function ReviewStep({ formData, onChange }: StepProps) {
  return (
    <div className="wizard-fields review-step">
      {/* Summary Cards */}
      <div className="review-section">
        <Heading level={3}>Basic Information</Heading>
        <div className="review-grid">
          <div className="review-item">
            <Text UNSAFE_className="impulse-label">Name</Text>
            <Text UNSAFE_className="impulse-value">
              {formData.firstName} {formData.lastName}
            </Text>
          </div>
          <div className="review-item">
            <Text UNSAFE_className="impulse-label">Date of Birth</Text>
            <Text UNSAFE_className="impulse-value">{formData.dateOfBirth || '—'}</Text>
          </div>
          <div className="review-item">
            <Text UNSAFE_className="impulse-label">Admission Date</Text>
            <Text UNSAFE_className="impulse-value">{formData.admissionDate || '—'}</Text>
          </div>
          <div className="review-item">
            <Text UNSAFE_className="impulse-label">Room Preference</Text>
            <Text UNSAFE_className="impulse-value">{formData.roomPreference || 'No Preference'}</Text>
          </div>
        </div>
      </div>

      <Divider />

      <div className="review-section">
        <Heading level={3}>Medical Information</Heading>
        <div className="review-grid">
          <div className="review-item">
            <Text UNSAFE_className="impulse-label">Allergies</Text>
            <Text UNSAFE_className="impulse-value">
              {(formData.allergies as string[])?.join(', ') || 'None listed'}
            </Text>
          </div>
          <div className="review-item">
            <Text UNSAFE_className="impulse-label">Medications</Text>
            <Text UNSAFE_className="impulse-value">
              {(formData.currentMedications as string[])?.join(', ') || 'None listed'}
            </Text>
          </div>
          <div className="review-item">
            <Text UNSAFE_className="impulse-label">Physician</Text>
            <Text UNSAFE_className="impulse-value">{formData.primaryCarePhysician || '—'}</Text>
          </div>
        </div>
      </div>

      <Divider />

      <div className="review-section">
        <Heading level={3}>Care Preferences</Heading>
        <div className="review-grid">
          <div className="review-item">
            <Text UNSAFE_className="impulse-label">Mobility</Text>
            <Text UNSAFE_className="impulse-value">{formData.mobilityLevel || '—'}</Text>
          </div>
          <div className="review-item">
            <Text UNSAFE_className="impulse-label">Communication</Text>
            <Text UNSAFE_className="impulse-value">{formData.communicationPreference || '—'}</Text>
          </div>
        </div>
      </div>

      <Divider />

      <div className="review-section">
        <Heading level={3}>Emergency Contacts</Heading>
        {(formData.contacts as EmergencyContact[])?.map((c, i) => (
          <div key={i} className="review-contact">
            <Text UNSAFE_className="impulse-value">
              {c.name} ({c.relationship}) - {c.phone}
              {c.isPrimaryContact && <Badge variant="positive" size="S">Primary</Badge>}
            </Text>
          </div>
        ))}
      </div>

      <Divider />

      {/* Consent Checkboxes */}
      <div className="consent-section">
        <Heading level={3}>Consent & Authorization</Heading>
        <div className="consent-checkboxes">
          <Checkbox
            name="acceptsTerms"
            isSelected={formData.acceptsTerms as boolean}
            onChange={(v) => onChange('acceptsTerms', v)}
            isRequired
          >
            I accept the facility terms and conditions *
          </Checkbox>
          <Checkbox
            name="authorizesRelease"
            isSelected={formData.authorizesRelease as boolean}
            onChange={(v) => onChange('authorizesRelease', v)}
            isRequired
          >
            I authorize the release of medical information to authorized personnel *
          </Checkbox>
        </div>
      </div>

      <TextField
        label="Additional Comments"
        name="additionalComments"
        value={formData.additionalComments as string}
        onChange={(v) => onChange('additionalComments', v)}
      />
    </div>
  );
}

// ========================================
// Wizard Page - Standalone route
// ========================================

export function AdmissionWizardPage() {
  return (
    <div className="form-container">
      <AdmissionWizard />
    </div>
  );
}
