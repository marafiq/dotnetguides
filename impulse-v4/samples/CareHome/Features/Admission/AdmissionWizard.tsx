import { useState, useEffect, useCallback } from 'react';
import { useNavigate } from '@tanstack/react-router';
import { Plus, Trash2, Check } from 'lucide-react';
import { ZodError } from 'zod';
import { useImpulse } from '../../client/shared/ImpulseProvider';
import { ImpulseValidationError } from '../../client/impulse-runtime';
import { Button } from '../../client/components/ui/button';
import { Card, CardContent, CardHeader, CardTitle, CardDescription } from '../../client/components/ui/card';
import { Input } from '../../client/components/ui/input';
import { Label } from '../../client/components/ui/label';
import { Checkbox } from '../../client/components/ui/checkbox';
import { Progress } from '../../client/components/ui/progress';
import { Badge } from '../../client/components/ui/badge';
import { Separator } from '../../client/components/ui/separator';
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from '../../client/components/ui/select';

// ========================================
// Impulse Way: Import from GENERATED files
// ========================================
import {
  DietaryRequirement,
  type GetAdmissionWizardResponse,
  type WizardEmergencyContact,
  type BasicInfoStepRequest,
  type MedicalHistoryStepRequest,
  type CarePreferencesStepRequest,
  type EmergencyContactsStepRequest,
  type CompleteAdmissionResponse,
  type CareLevel,
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
    return (
      <div className="flex items-center justify-center h-64">
        <div className="animate-spin rounded-full h-8 w-8 border-b-2 border-primary"></div>
      </div>
    );
  }

  const step = config.steps[currentStep - 1];
  const progress = (currentStep / config.totalSteps) * 100;

  return (
    <div className="max-w-3xl mx-auto space-y-6" data-testid="admission-wizard">
      {/* Header */}
      <div className="text-center">
        <h1 className="text-3xl font-bold tracking-tight">{config.title}</h1>
        <p className="text-muted-foreground mt-2">{config.description}</p>
      </div>

      {/* Progress */}
      <div className="space-y-4">
        <div className="flex items-center justify-between text-sm text-muted-foreground">
          <span>Step {currentStep} of {config.totalSteps}</span>
          <span>{Math.round(progress)}% complete</span>
        </div>
        <Progress value={progress} className="h-2" />

        {/* Step indicators */}
        <div className="flex justify-between">
          {config.steps.map((s, i) => {
            const stepNum = i + 1;
            const isActive = stepNum === currentStep;
            const isCompleted = completedSteps.has(stepNum);

            return (
              <div
                key={s.id}
                className={`flex flex-col items-center gap-1 ${isActive ? 'text-primary' : isCompleted ? 'text-green-600' : 'text-muted-foreground'}`}
                data-testid={isActive ? `${s.id}-step` : undefined}
              >
                <div
                  className={`w-8 h-8 rounded-full flex items-center justify-center text-sm font-medium border-2 transition-colors ${
                    isActive
                      ? 'border-primary bg-primary text-primary-foreground'
                      : isCompleted
                      ? 'border-green-600 bg-green-600 text-white'
                      : 'border-muted-foreground/30 bg-background'
                  }`}
                >
                  {isCompleted ? <Check className="h-4 w-4" /> : stepNum}
                </div>
                <span className="text-xs font-medium hidden sm:block">{s.title}</span>
              </div>
            );
          })}
        </div>
      </div>

      {/* Step Card */}
      <Card>
        <CardHeader>
          <div className="flex items-center gap-2">
            <Badge variant="info">Step {currentStep}</Badge>
          </div>
          <CardTitle className="text-xl">{step.title}</CardTitle>
          <CardDescription>{step.description}</CardDescription>
        </CardHeader>

        <Separator />

        <CardContent className="pt-6">
          {errors.form && (
            <div className="mb-6 p-3 rounded-md bg-destructive/10 border border-destructive/20">
              <p className="text-sm text-destructive">{errors.form}</p>
            </div>
          )}

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
        </CardContent>

        <Separator />

        <div className="p-6 flex justify-between">
          <Button
            variant="outline"
            onClick={handleBack}
            disabled={currentStep === 1}
            data-testid="back-button"
          >
            Back
          </Button>

          {currentStep < config.totalSteps ? (
            <Button
              onClick={handleNext}
              isLoading={isValidating}
              data-testid="next-button"
            >
              {isValidating ? 'Validating...' : 'Next'}
            </Button>
          ) : (
            <Button
              onClick={handleSubmit}
              isLoading={isValidating}
              data-testid="next-button"
            >
              {isValidating ? 'Submitting...' : 'Complete Admission'}
            </Button>
          )}
        </div>
      </Card>
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

function BasicInfoStep({ formData, errors, onChange }: StepProps) {
  return (
    <div className="space-y-4">
      <div className="grid gap-4 sm:grid-cols-2">
        <div className="space-y-2">
          <Label htmlFor="firstName">
            First Name <span className="text-destructive">*</span>
          </Label>
          <Input
            id="firstName"
            name="firstName"
            value={formData.firstName as string}
            onChange={(e) => onChange('firstName', e.target.value)}
            error={errors.firstName}
            data-testid="input-firstName"
          />
          {errors.firstName && (
            <p className="text-xs text-destructive">{errors.firstName}</p>
          )}
        </div>
        <div className="space-y-2">
          <Label htmlFor="lastName">
            Last Name <span className="text-destructive">*</span>
          </Label>
          <Input
            id="lastName"
            name="lastName"
            value={formData.lastName as string}
            onChange={(e) => onChange('lastName', e.target.value)}
            error={errors.lastName}
            data-testid="input-lastName"
          />
          {errors.lastName && (
            <p className="text-xs text-destructive">{errors.lastName}</p>
          )}
        </div>
      </div>

      <div className="space-y-2">
        <Label htmlFor="dateOfBirth">
          Date of Birth <span className="text-destructive">*</span>
        </Label>
        <Input
          id="dateOfBirth"
          name="dateOfBirth"
          type="date"
          value={formData.dateOfBirth as string}
          onChange={(e) => onChange('dateOfBirth', e.target.value)}
          error={errors.dateOfBirth}
          data-testid="input-dateOfBirth"
        />
        {errors.dateOfBirth && (
          <p className="text-xs text-destructive">{errors.dateOfBirth}</p>
        )}
      </div>

      <div className="space-y-2">
        <Label htmlFor="roomPreference">Room Preference</Label>
        <Input
          id="roomPreference"
          name="roomPreference"
          value={formData.roomPreference as string}
          onChange={(e) => onChange('roomPreference', e.target.value)}
          placeholder="e.g., Ground floor, near garden"
        />
      </div>
    </div>
  );
}

function MedicalHistoryStep({ formData, errors, onChange }: StepProps) {
  return (
    <div className="space-y-4">
      <div className="space-y-2">
        <Label htmlFor="existingConditions">Existing Conditions</Label>
        <textarea
          id="existingConditions"
          name="existingConditions"
          className="flex min-h-[100px] w-full rounded-md border border-input bg-transparent px-3 py-2 text-sm shadow-sm placeholder:text-muted-foreground focus-visible:outline-none focus-visible:ring-1 focus-visible:ring-ring disabled:cursor-not-allowed disabled:opacity-50"
          value={(formData.existingConditions as string[]).join('\n')}
          onChange={(e) => onChange('existingConditions', e.target.value.split('\n').filter(Boolean))}
          placeholder="Enter one condition per line"
          data-testid="input-conditions"
        />
        <p className="text-xs text-muted-foreground">One condition per line</p>
      </div>

      <div className="space-y-2">
        <Label htmlFor="currentMedications">Current Medications</Label>
        <textarea
          id="currentMedications"
          name="currentMedications"
          className="flex min-h-[100px] w-full rounded-md border border-input bg-transparent px-3 py-2 text-sm shadow-sm placeholder:text-muted-foreground focus-visible:outline-none focus-visible:ring-1 focus-visible:ring-ring disabled:cursor-not-allowed disabled:opacity-50"
          value={(formData.currentMedications as string[]).join('\n')}
          onChange={(e) => onChange('currentMedications', e.target.value.split('\n').filter(Boolean))}
          placeholder="Enter one medication per line"
          data-testid="input-medications"
        />
        <p className="text-xs text-muted-foreground">One medication per line</p>
      </div>

      <div className="space-y-2">
        <Label htmlFor="primaryCarePhysician">Primary Care Physician</Label>
        <Input
          id="primaryCarePhysician"
          name="primaryCarePhysician"
          value={formData.primaryCarePhysician as string}
          onChange={(e) => onChange('primaryCarePhysician', e.target.value)}
          placeholder="Dr. Smith"
        />
      </div>
    </div>
  );
}

function CarePreferencesStep({ formData, errors, onChange }: StepProps) {
  const dietaryOptions: { value: DietaryRequirement; label: string }[] = [
    { value: DietaryRequirement.Regular, label: 'Regular' },
    { value: DietaryRequirement.Diabetic, label: 'Diabetic' },
    { value: DietaryRequirement.LowSodium, label: 'Low Sodium' },
    { value: DietaryRequirement.Vegetarian, label: 'Vegetarian' },
    { value: DietaryRequirement.GlutenFree, label: 'Gluten Free' },
  ];

  const toggleDietary = (value: DietaryRequirement) => {
    const current = formData.dietaryRequirements as DietaryRequirement[];
    if (current.includes(value)) {
      onChange('dietaryRequirements', current.filter(v => v !== value));
    } else {
      onChange('dietaryRequirements', [...current, value]);
    }
  };

  return (
    <div className="space-y-6">
      <div className="space-y-2">
        <Label htmlFor="careLevel">
          Care Level <span className="text-destructive">*</span>
        </Label>
        <Select
          value={formData.careLevel as string}
          onValueChange={(value) => onChange('careLevel', value)}
        >
          <SelectTrigger data-testid="input-careLevel">
            <SelectValue placeholder="Select care level" />
          </SelectTrigger>
          <SelectContent>
            <SelectItem value="Independent">Independent</SelectItem>
            <SelectItem value="Assisted">Assisted</SelectItem>
            <SelectItem value="FullCare">Full Care</SelectItem>
            <SelectItem value="Memory">Memory Care</SelectItem>
          </SelectContent>
        </Select>
      </div>

      <div className="space-y-3">
        <Label>Dietary Requirements</Label>
        <div className="grid gap-3 sm:grid-cols-2">
          {dietaryOptions.map((option) => (
            <div key={option.value} className="flex items-center space-x-2">
              <Checkbox
                id={`dietary-${option.value}`}
                checked={(formData.dietaryRequirements as string[]).includes(option.value)}
                onCheckedChange={() => toggleDietary(option.value)}
                data-testid={option.value === 'LowSodium' ? 'checkbox-LowSodium' : undefined}
              />
              <Label
                htmlFor={`dietary-${option.value}`}
                className="text-sm font-normal cursor-pointer"
              >
                {option.label}
              </Label>
            </div>
          ))}
        </div>
      </div>

      <div className="flex items-center space-x-2">
        <Checkbox
          id="requiresNightChecks"
          checked={formData.requiresNightChecks as boolean}
          onCheckedChange={(checked) => onChange('requiresNightChecks', checked)}
          data-testid="checkbox-requiresNightChecks"
        />
        <Label htmlFor="requiresNightChecks" className="text-sm font-normal cursor-pointer">
          Requires Night Checks
        </Label>
      </div>
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

function EmergencyContactsStep({ contacts, errors, onAdd, onUpdate, onRemove }: EmergencyContactsStepProps) {
  return (
    <div className="space-y-4">
      <div className="flex items-center justify-between">
        <div>
          <h4 className="font-medium">Emergency Contacts</h4>
          <p className="text-sm text-muted-foreground">At least one contact is required</p>
        </div>
        <Button variant="outline" size="sm" onClick={onAdd}>
          <Plus className="h-4 w-4 mr-1" />
          Add Contact
        </Button>
      </div>

      {contacts.length === 0 && (
        <div className="text-center py-8 border-2 border-dashed rounded-lg">
          <p className="text-muted-foreground">No emergency contacts added yet.</p>
          <Button variant="outline" size="sm" onClick={onAdd} className="mt-2">
            <Plus className="h-4 w-4 mr-1" />
            Add First Contact
          </Button>
        </div>
      )}

      {contacts.map((contact, index) => (
        <Card key={index} className="bg-muted/50">
          <CardContent className="pt-4">
            <div className="flex items-center justify-between mb-4">
              <Badge variant={contact.isPrimaryContact ? 'success' : 'secondary'}>
                {contact.isPrimaryContact ? 'Primary Contact' : `Contact ${index + 1}`}
              </Badge>
              <Button
                variant="ghost"
                size="sm"
                onClick={() => onRemove(index)}
                className="text-destructive hover:text-destructive"
              >
                <Trash2 className="h-4 w-4" />
              </Button>
            </div>

            <div className="grid gap-4 sm:grid-cols-2">
              <div className="space-y-2">
                <Label htmlFor={`contact-name-${index}`}>
                  Name <span className="text-destructive">*</span>
                </Label>
                <Input
                  id={`contact-name-${index}`}
                  value={contact.name}
                  onChange={(e) => onUpdate(index, 'name', e.target.value)}
                  data-testid="input-primaryContactName"
                />
              </div>
              <div className="space-y-2">
                <Label htmlFor={`contact-relationship-${index}`}>
                  Relationship <span className="text-destructive">*</span>
                </Label>
                <Input
                  id={`contact-relationship-${index}`}
                  value={contact.relationship}
                  onChange={(e) => onUpdate(index, 'relationship', e.target.value)}
                  placeholder="e.g., Son, Daughter"
                  data-testid="input-primaryContactRelationship"
                />
              </div>
            </div>

            <div className="mt-4 space-y-2">
              <Label htmlFor={`contact-phone-${index}`}>
                Phone <span className="text-destructive">*</span>
              </Label>
              <Input
                id={`contact-phone-${index}`}
                value={contact.phone}
                onChange={(e) => onUpdate(index, 'phone', e.target.value)}
                placeholder="(555) 123-4567"
                data-testid="input-primaryContactPhone"
              />
            </div>
          </CardContent>
        </Card>
      ))}
    </div>
  );
}

function ReviewStep({ formData, errors, onChange }: StepProps) {
  return (
    <div className="space-y-6">
      {/* Basic Information Summary */}
      <div className="space-y-3">
        <h3 className="font-semibold">Basic Information</h3>
        <div className="grid gap-2 sm:grid-cols-2 text-sm">
          <div>
            <span className="text-muted-foreground">Name: </span>
            <span className="font-medium">
              {String(formData.firstName || '')} {String(formData.lastName || '')}
            </span>
          </div>
          <div>
            <span className="text-muted-foreground">Date of Birth: </span>
            <span className="font-medium">{String(formData.dateOfBirth) || '—'}</span>
          </div>
        </div>
      </div>

      <Separator />

      {/* Care Preferences Summary */}
      <div className="space-y-3">
        <h3 className="font-semibold">Care Preferences</h3>
        <div className="text-sm">
          <span className="text-muted-foreground">Care Level: </span>
          <Badge variant="info">{String(formData.careLevel || '')}</Badge>
        </div>
      </div>

      <Separator />

      {/* Consent */}
      <div className="space-y-4">
        <h3 className="font-semibold">Consent & Authorization</h3>

        <div className="space-y-3">
          <div className="flex items-start space-x-2">
            <Checkbox
              id="acceptsTerms"
              checked={formData.acceptsTerms as boolean}
              onCheckedChange={(checked) => onChange('acceptsTerms', checked)}
              data-testid="checkbox-consent"
            />
            <div className="grid gap-1.5 leading-none">
              <Label htmlFor="acceptsTerms" className="text-sm font-normal cursor-pointer">
                I accept the terms and conditions <span className="text-destructive">*</span>
              </Label>
              {errors.acceptsTerms && (
                <p className="text-xs text-destructive">{errors.acceptsTerms}</p>
              )}
            </div>
          </div>

          <div className="flex items-start space-x-2">
            <Checkbox
              id="authorizesRelease"
              checked={formData.authorizesRelease as boolean}
              onCheckedChange={(checked) => onChange('authorizesRelease', checked)}
              data-testid="checkbox-terms"
            />
            <div className="grid gap-1.5 leading-none">
              <Label htmlFor="authorizesRelease" className="text-sm font-normal cursor-pointer">
                I authorize release of information <span className="text-destructive">*</span>
              </Label>
              {errors.authorizesRelease && (
                <p className="text-xs text-destructive">{errors.authorizesRelease}</p>
              )}
            </div>
          </div>
        </div>
      </div>
    </div>
  );
}

// ========================================
// Page Component
// ========================================

export function AdmissionWizardPage() {
  return (
    <div className="py-6">
      <AdmissionWizard />
    </div>
  );
}
