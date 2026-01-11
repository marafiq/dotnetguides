import { useState, useEffect, useCallback } from 'react';
import { useNavigate } from '@tanstack/react-router';
import {
  View,
  Flex,
  Button,
  Heading,
  Text,
  TextField,
  Checkbox,
  CheckboxGroup,
  Picker,
  Item,
  Divider,
  ProgressBar,
  Badge,
} from '@adobe/react-spectrum';
import { ZodError } from 'zod';
import { useImpulse } from '../../client/shared/ImpulseProvider';
import { ImpulseValidationError } from '../../client/impulse-runtime';

// ========================================
// Impulse Way: Import from GENERATED files
// ========================================
import type {
  GetAdmissionWizardResponse,
  WizardEmergencyContact,
  BasicInfoStepRequest,
  MedicalHistoryStepRequest,
  CarePreferencesStepRequest,
  EmergencyContactsStepRequest,
  CompleteAdmissionResponse,
  CareLevel,
  DietaryRequirement,
} from '../../generated/types';

import {
  BasicInfoStepRequestSchema,
  MedicalHistoryStepRequestSchema,
  CarePreferencesStepRequestSchema,
  EmergencyContactsStepRequestSchema,
} from '../../generated/validation';

import {
  useValidateBasicInfoMutation,
  useValidateMedicalHistoryMutation,
  useValidateCarePreferencesMutation,
  useValidateEmergencyContactsMutation,
  useCompleteAdmissionMutation,
} from '../../generated/mutations';

import { z } from 'zod';

// Review schema - only local schema for final step
const ReviewSchema = z.object({
  acceptsTerms: z.literal(true, { errorMap: () => ({ message: 'You must accept the terms' }) }),
  authorizesRelease: z.literal(true, { errorMap: () => ({ message: 'Authorization is required' }) }),
});

// Map step IDs to their generated schemas
const stepSchemas: Record<string, z.ZodSchema> = {
  'basic-info': BasicInfoStepRequestSchema,
  'medical-history': MedicalHistoryStepRequestSchema,
  'care-preferences': CarePreferencesStepRequestSchema,
  'emergency-contacts': EmergencyContactsStepRequestSchema,
  'review': ReviewSchema,
};

// ========================================
// Wizard Component - Uses Generated Code
// ========================================

export function AdmissionWizard() {
  const ctx = useImpulse();
  const navigate = useNavigate();

  // Use generated mutation hooks
  const validateBasicInfo = useValidateBasicInfoMutation(ctx);
  const validateMedicalHistory = useValidateMedicalHistoryMutation(ctx);
  const validateCarePreferences = useValidateCarePreferencesMutation(ctx);
  const validateEmergencyContacts = useValidateEmergencyContactsMutation(ctx);
  const completeAdmission = useCompleteAdmissionMutation(ctx);

  // State
  const [config, setConfig] = useState<GetAdmissionWizardResponse | null>(null);
  const [currentStep, setCurrentStep] = useState(1);
  const [errors, setErrors] = useState<Record<string, string>>({});
  const [isLoading, setIsLoading] = useState(true);
  const [isValidating, setIsValidating] = useState(false);
  const [completedSteps, setCompletedSteps] = useState<Set<number>>(new Set());

  // Form data - matches generated types
  const [formData, setFormData] = useState({
    // Step 1: Basic Info
    firstName: '',
    lastName: '',
    dateOfBirth: '',
    roomPreference: '',
    // Step 2: Medical History
    existingConditions: [] as string[],
    allergies: [] as string[],
    currentMedications: [] as string[],
    primaryCarePhysician: '',
    specialInstructions: '',
    // Step 3: Care Preferences
    careLevel: 'Independent' as CareLevel,
    dietaryRequirements: [] as DietaryRequirement[],
    requiresNightChecks: false,
    additionalNotes: '',
    // Step 4: Emergency Contacts
    contacts: [] as WizardEmergencyContact[],
    // Step 5: Review
    acceptsTerms: false,
    authorizesRelease: false,
  });

  // Load wizard configuration
  useEffect(() => {
    async function loadConfig() {
      try {
        const response = await ctx.impulse<GetAdmissionWizardResponse>('/admission/wizard');
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
    if (errors[field]) {
      setErrors(prev => {
        const next = { ...prev };
        delete next[field];
        return next;
      });
    }
  }, [errors]);

  // Get current step data
  const getStepData = useCallback((stepId: string) => {
    switch (stepId) {
      case 'basic-info':
        return {
          firstName: formData.firstName,
          lastName: formData.lastName,
          dateOfBirth: formData.dateOfBirth,
          roomPreference: formData.roomPreference || null,
        } satisfies BasicInfoStepRequest;
      case 'medical-history':
        return {
          existingConditions: formData.existingConditions,
          allergies: formData.allergies,
          currentMedications: formData.currentMedications,
          primaryCarePhysician: formData.primaryCarePhysician || null,
          specialInstructions: formData.specialInstructions || null,
        } satisfies MedicalHistoryStepRequest;
      case 'care-preferences':
        return {
          careLevel: formData.careLevel,
          dietaryRequirements: formData.dietaryRequirements,
          requiresNightChecks: formData.requiresNightChecks,
          additionalNotes: formData.additionalNotes || null,
        } satisfies CarePreferencesStepRequest;
      case 'emergency-contacts':
        return {
          contacts: formData.contacts,
        } satisfies EmergencyContactsStepRequest;
      case 'review':
        return {
          acceptsTerms: formData.acceptsTerms,
          authorizesRelease: formData.authorizesRelease,
        };
      default:
        return {};
    }
  }, [formData]);

  // Validate step
  const validateStep = useCallback(async (stepId: string): Promise<boolean> => {
    setErrors({});
    setIsValidating(true);

    const stepData = getStepData(stepId);
    const schema = stepSchemas[stepId];

    try {
      // Client-side validation with generated Zod schema
      if (schema) {
        schema.parse(stepData);
      }

      // Server-side validation using generated mutation hooks
      switch (stepId) {
        case 'basic-info':
          await validateBasicInfo.mutateAsync(stepData as BasicInfoStepRequest);
          break;
        case 'medical-history':
          await validateMedicalHistory.mutateAsync(stepData as MedicalHistoryStepRequest);
          break;
        case 'care-preferences':
          await validateCarePreferences.mutateAsync(stepData as CarePreferencesStepRequest);
          break;
        case 'emergency-contacts':
          await validateEmergencyContacts.mutateAsync(stepData as EmergencyContactsStepRequest);
          break;
      }

      return true;
    } catch (err) {
      if (err instanceof ZodError) {
        const fieldErrors: Record<string, string> = {};
        err.errors.forEach(error => {
          const path = error.path.join('.');
          fieldErrors[path] = error.message;
        });
        setErrors(fieldErrors);
      } else if (err instanceof ImpulseValidationError) {
        setErrors(err.fieldErrors);
      }
      return false;
    } finally {
      setIsValidating(false);
    }
  }, [getStepData, validateBasicInfo, validateMedicalHistory, validateCarePreferences, validateEmergencyContacts]);

  // Navigate
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

  const handleBack = useCallback(() => {
    if (currentStep > 1) {
      setCurrentStep(prev => prev - 1);
      setErrors({});
    }
  }, [currentStep]);

  // Submit
  const handleSubmit = useCallback(async () => {
    if (!config) return;

    const step = config.steps[currentStep - 1];
    const isValid = await validateStep(step.id);
    if (!isValid) return;

    setIsValidating(true);
    try {
      const result: CompleteAdmissionResponse = await completeAdmission.mutateAsync({
        basicInfo: getStepData('basic-info') as BasicInfoStepRequest,
        medicalHistory: getStepData('medical-history') as MedicalHistoryStepRequest,
        carePreferences: getStepData('care-preferences') as CarePreferencesStepRequest,
        emergencyContacts: getStepData('emergency-contacts') as EmergencyContactsStepRequest,
        acceptsTerms: formData.acceptsTerms,
        authorizesRelease: formData.authorizesRelease,
      });

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
  }, [config, currentStep, validateStep, completeAdmission, formData, getStepData, navigate]);

  // Add/update/remove emergency contact
  const addContact = useCallback(() => {
    setFormData(prev => ({
      ...prev,
      contacts: [
        ...prev.contacts,
        { name: '', relationship: '', phone: '', email: null, isPrimaryContact: prev.contacts.length === 0 },
      ],
    }));
  }, []);

  const updateContact = useCallback((index: number, field: keyof WizardEmergencyContact, value: unknown) => {
    setFormData(prev => ({
      ...prev,
      contacts: prev.contacts.map((c, i) =>
        i === index ? { ...c, [field]: value } : c
      ),
    }));
  }, []);

  const removeContact = useCallback((index: number) => {
    setFormData(prev => ({
      ...prev,
      contacts: prev.contacts.filter((_, i) => i !== index),
    }));
  }, []);

  // Loading
  if (isLoading || !config) {
    return <Text>Loading wizard...</Text>;
  }

  const step = config.steps[currentStep - 1];
  const progress = (currentStep / config.totalSteps) * 100;

  return (
    <div className="wizard-container" data-testid="admission-wizard">
      <header className="wizard-header">
        <Heading level={1}>{config.title}</Heading>
        <Text>{config.description}</Text>
      </header>

      <div className="wizard-progress">
        <ProgressBar
          label={`Step ${currentStep} of ${config.totalSteps}`}
          value={progress}
        />
        <div className="wizard-steps-nav">
          {config.steps.map((s, i) => (
            <div
              key={s.id}
              className={`wizard-step-indicator ${i + 1 === currentStep ? 'active' : ''} ${completedSteps.has(i + 1) ? 'completed' : ''}`}
              data-testid={i + 1 === currentStep ? `${s.id}-step` : undefined}
            >
              <span className="step-number">{completedSteps.has(i + 1) ? '✓' : i + 1}</span>
              <span className="step-title">{s.title}</span>
            </div>
          ))}
        </div>
      </div>

      <View UNSAFE_className="wizard-card" backgroundColor="gray-50" padding="size-400" borderRadius="medium">
        <div className="wizard-step-header">
          <Badge variant="info">Step {currentStep}</Badge>
          <Heading level={2}>{step.title}</Heading>
          <Text UNSAFE_className="step-description">{step.description}</Text>
        </div>

        <Divider />

        {errors.form && (
          <div className="form-error">
            <Text UNSAFE_className="error-text">{errors.form}</Text>
          </div>
        )}

        <div className="wizard-step-content">
          {step.id === 'basic-info' && (
            <BasicInfoStep formData={formData} errors={errors} onChange={handleChange} />
          )}
          {step.id === 'medical-history' && (
            <MedicalHistoryStep formData={formData} errors={errors} onChange={handleChange} />
          )}
          {step.id === 'care-preferences' && (
            <CarePreferencesStep formData={formData} errors={errors} onChange={handleChange} />
          )}
          {step.id === 'emergency-contacts' && (
            <EmergencyContactsStep
              contacts={formData.contacts}
              errors={errors}
              onAdd={addContact}
              onUpdate={updateContact}
              onRemove={removeContact}
            />
          )}
          {step.id === 'review' && (
            <ReviewStep formData={formData} errors={errors} onChange={handleChange} />
          )}
        </div>

        <Divider />

        <div className="wizard-actions">
          <Button
            variant="secondary"
            onPress={handleBack}
            isDisabled={currentStep === 1}
            data-testid="back-button"
          >
            Back
          </Button>

          {currentStep < config.totalSteps ? (
            <Button
              variant="accent"
              onPress={handleNext}
              isPending={isValidating}
              data-testid="next-button"
            >
              {isValidating ? 'Validating...' : 'Next'}
            </Button>
          ) : (
            <Button
              variant="accent"
              onPress={handleSubmit}
              isPending={isValidating}
              data-testid="next-button"
            >
              {isValidating ? 'Submitting...' : 'Complete Admission'}
            </Button>
          )}
        </div>
      </View>
    </div>
  );
}

// ========================================
// Step Components
// ========================================

interface StepProps {
  formData: Record<string, unknown>;
  errors: Record<string, string>;
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
          data-testid="input-firstName"
        />
        <TextField
          label="Last Name *"
          name="lastName"
          value={formData.lastName as string}
          onChange={(v) => onChange('lastName', v)}
          isRequired
          data-testid="input-lastName"
        />
      </div>
      <TextField
        label="Date of Birth *"
        name="dateOfBirth"
        type="date"
        value={formData.dateOfBirth as string}
        onChange={(v) => onChange('dateOfBirth', v)}
        isRequired
        data-testid="input-dateOfBirth"
      />
      <TextField
        label="Room Preference"
        name="roomPreference"
        value={formData.roomPreference as string}
        onChange={(v) => onChange('roomPreference', v)}
      />
    </div>
  );
}

function MedicalHistoryStep({ formData, onChange }: StepProps) {
  return (
    <div className="wizard-fields">
      <TextField
        label="Existing Conditions"
        name="conditions"
        value={(formData.existingConditions as string[]).join('\n')}
        onChange={(v) => onChange('existingConditions', v.split('\n').filter(Boolean))}
        description="One per line"
        data-testid="input-conditions"
      />
      <TextField
        label="Current Medications"
        name="medications"
        value={(formData.currentMedications as string[]).join('\n')}
        onChange={(v) => onChange('currentMedications', v.split('\n').filter(Boolean))}
        description="One per line"
        data-testid="input-medications"
      />
      <TextField
        label="Primary Care Physician"
        name="primaryCarePhysician"
        value={formData.primaryCarePhysician as string}
        onChange={(v) => onChange('primaryCarePhysician', v)}
      />
    </div>
  );
}

function CarePreferencesStep({ formData, onChange }: StepProps) {
  return (
    <div className="wizard-fields">
      <Picker
        label="Care Level *"
        selectedKey={formData.careLevel as string}
        onSelectionChange={(key) => onChange('careLevel', key)}
        data-testid="input-careLevel"
      >
        <Item key="Independent">Independent</Item>
        <Item key="Assisted">Assisted</Item>
        <Item key="FullCare">Full Care</Item>
        <Item key="Memory">Memory Care</Item>
      </Picker>

      <CheckboxGroup
        label="Dietary Requirements"
        value={formData.dietaryRequirements as string[]}
        onChange={(v) => onChange('dietaryRequirements', v)}
      >
        <Checkbox value="Regular">Regular</Checkbox>
        <Checkbox value="Diabetic">Diabetic</Checkbox>
        <Checkbox value="LowSodium" data-testid="checkbox-LowSodium">Low Sodium</Checkbox>
        <Checkbox value="Vegetarian">Vegetarian</Checkbox>
        <Checkbox value="GlutenFree">Gluten Free</Checkbox>
      </CheckboxGroup>

      <Checkbox
        name="requiresNightChecks"
        isSelected={formData.requiresNightChecks as boolean}
        onChange={(v) => onChange('requiresNightChecks', v)}
        data-testid="checkbox-requiresNightChecks"
      >
        Requires Night Checks
      </Checkbox>
    </div>
  );
}

interface EmergencyContactsStepProps {
  contacts: WizardEmergencyContact[];
  errors: Record<string, string>;
  onAdd: () => void;
  onUpdate: (index: number, field: keyof WizardEmergencyContact, value: unknown) => void;
  onRemove: (index: number) => void;
}

function EmergencyContactsStep({ contacts, onAdd, onUpdate, onRemove }: EmergencyContactsStepProps) {
  return (
    <div className="wizard-fields">
      <div className="contacts-header">
        <Text>Emergency Contacts</Text>
        <Button variant="secondary" onPress={onAdd}>+ Add Contact</Button>
      </div>

      {contacts.length === 0 && (
        <Text>No emergency contacts added. At least one required.</Text>
      )}

      {contacts.map((contact, index) => (
        <View key={index} UNSAFE_className="contact-card" backgroundColor="gray-100" padding="size-200" borderRadius="small">
          <div className="contact-header">
            <Badge variant={contact.isPrimaryContact ? 'positive' : 'neutral'}>
              {contact.isPrimaryContact ? 'Primary' : `Contact ${index + 1}`}
            </Badge>
            <Button variant="secondary" onPress={() => onRemove(index)}>Remove</Button>
          </div>

          <div className="form-row">
            <TextField
              label="Name *"
              name={`contacts.${index}.name`}
              value={contact.name}
              onChange={(v) => onUpdate(index, 'name', v)}
              isRequired
              data-testid="input-primaryContactName"
            />
            <TextField
              label="Relationship *"
              name={`contacts.${index}.relationship`}
              value={contact.relationship}
              onChange={(v) => onUpdate(index, 'relationship', v)}
              isRequired
              data-testid="input-primaryContactRelationship"
            />
          </div>
          <TextField
            label="Phone *"
            name={`contacts.${index}.phone`}
            value={contact.phone}
            onChange={(v) => onUpdate(index, 'phone', v)}
            isRequired
            data-testid="input-primaryContactPhone"
          />
        </View>
      ))}
    </div>
  );
}

function ReviewStep({ formData, onChange }: StepProps) {
  return (
    <div className="wizard-fields review-step">
      <div className="review-section">
        <Heading level={3}>Basic Information</Heading>
        <div className="review-grid">
          <div className="review-item">
            <Text UNSAFE_className="label">Name</Text>
            <Text>{String(formData.firstName || '')} {String(formData.lastName || '')}</Text>
          </div>
          <div className="review-item">
            <Text UNSAFE_className="label">Date of Birth</Text>
            <Text>{String(formData.dateOfBirth) || '—'}</Text>
          </div>
        </div>
      </div>

      <Divider />

      <div className="review-section">
        <Heading level={3}>Care Preferences</Heading>
        <div className="review-item">
          <Text UNSAFE_className="label">Care Level</Text>
          <Text>{String(formData.careLevel || '')}</Text>
        </div>
      </div>

      <Divider />

      <div className="consent-section">
        <Heading level={3}>Consent & Authorization</Heading>
        <Checkbox
          name="acceptsTerms"
          isSelected={formData.acceptsTerms as boolean}
          onChange={(v) => onChange('acceptsTerms', v)}
          isRequired
          data-testid="checkbox-consent"
        >
          I accept the terms and conditions *
        </Checkbox>
        <Checkbox
          name="authorizesRelease"
          isSelected={formData.authorizesRelease as boolean}
          onChange={(v) => onChange('authorizesRelease', v)}
          isRequired
          data-testid="checkbox-terms"
        >
          I authorize release of information *
        </Checkbox>
      </div>
    </div>
  );
}

// ========================================
// Page Component
// ========================================

export function AdmissionWizardPage() {
  return (
    <div className="form-container">
      <AdmissionWizard />
    </div>
  );
}
