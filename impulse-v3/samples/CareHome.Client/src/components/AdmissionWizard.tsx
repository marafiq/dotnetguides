import { useState } from 'react';
import { useMutation } from '@tanstack/react-query';
import { useImpulse, useImpulsePage, isValidationError } from '@impulse/react';

// Types would be generated, but showing inline for demo
interface WizardStepConfig {
  id: string;
  title: string;
  description: string;
}

interface AdmissionWizardResponse {
  steps: WizardStepConfig[];
  careLevels: string[];
  dietaryRequirements: string[];
}

interface ValidateStepResponse {
  isValid: boolean;
  errors: Record<string, string[]> | null;
}

interface BasicInfo {
  firstName: string;
  lastName: string;
  dateOfBirth: string;
  roomPreference: string;
}

interface MedicalHistory {
  conditions: string[];
  medications: string[];
  allergies: string;
  primaryCarePhysician: string;
}

interface CarePreferences {
  careLevel: string;
  dietaryRequirements: string[];
  specialInstructions: string;
  requiresNightChecks: boolean;
}

interface EmergencyContacts {
  primaryContactName: string;
  primaryContactPhone: string;
  primaryContactRelationship: string;
  secondaryContactName: string;
  secondaryContactPhone: string;
}

interface AdmissionWizardProps {
  onComplete: () => void;
}

export function AdmissionWizard({ onComplete }: AdmissionWizardProps) {
  const { impulseMutate } = useImpulse();
  const { data: config, isLoading } = useImpulsePage<AdmissionWizardResponse>('/admission/wizard');

  const [currentStep, setCurrentStep] = useState(0);
  const [errors, setErrors] = useState<Record<string, string[]>>({});

  // Form state for each step
  const [basicInfo, setBasicInfo] = useState<BasicInfo>({
    firstName: '',
    lastName: '',
    dateOfBirth: '',
    roomPreference: '',
  });

  const [medicalHistory, setMedicalHistory] = useState<MedicalHistory>({
    conditions: [],
    medications: [],
    allergies: '',
    primaryCarePhysician: '',
  });

  const [carePreferences, setCarePreferences] = useState<CarePreferences>({
    careLevel: 'Independent',
    dietaryRequirements: [],
    specialInstructions: '',
    requiresNightChecks: false,
  });

  const [emergencyContacts, setEmergencyContacts] = useState<EmergencyContacts>({
    primaryContactName: '',
    primaryContactPhone: '',
    primaryContactRelationship: '',
    secondaryContactName: '',
    secondaryContactPhone: '',
  });

  const [consentGiven, setConsentGiven] = useState(false);
  const [termsAccepted, setTermsAccepted] = useState(false);

  // Validation mutation
  const validateMutation = useMutation({
    mutationFn: async (step: string) => {
      let data: unknown;
      switch (step) {
        case 'basic-info':
          data = basicInfo;
          break;
        case 'medical-history':
          data = medicalHistory;
          break;
        case 'care-preferences':
          data = carePreferences;
          break;
        case 'emergency-contacts':
          data = emergencyContacts;
          break;
        default:
          return { isValid: true, errors: null };
      }
      return impulseMutate<unknown, ValidateStepResponse>(
        `/admission/wizard/validate/${step}`,
        data,
        'POST'
      );
    },
  });

  // Complete admission mutation
  const completeMutation = useMutation({
    mutationFn: async () => {
      return impulseMutate<unknown, { residentId: number; roomAssigned: string; message: string }>(
        '/admission/wizard/complete',
        {
          basicInfo,
          medicalHistory,
          carePreferences,
          emergencyContacts,
          consentGiven,
          termsAccepted,
        },
        'POST'
      );
    },
    onSuccess: () => {
      onComplete();
    },
    onError: (error) => {
      if (isValidationError(error)) {
        setErrors(error.errors);
      }
    },
  });

  const handleNext = async () => {
    if (!config) return;

    const stepId = config.steps[currentStep].id;

    // Skip validation for review step
    if (stepId === 'review') {
      await completeMutation.mutateAsync();
      return;
    }

    const result = await validateMutation.mutateAsync(stepId);

    if (result.isValid) {
      setErrors({});
      setCurrentStep((prev) => Math.min(prev + 1, config.steps.length - 1));
    } else if (result.errors) {
      setErrors(result.errors);
    }
  };

  const handleBack = () => {
    setCurrentStep((prev) => Math.max(prev - 1, 0));
    setErrors({});
  };

  if (isLoading || !config) {
    return <div className="loading">Loading wizard...</div>;
  }

  const currentStepConfig = config.steps[currentStep];

  return (
    <div className="wizard">
      {/* Progress indicator */}
      <div className="wizard-progress">
        {config.steps.map((step, index) => (
          <div
            key={step.id}
            className={`wizard-step ${index === currentStep ? 'active' : ''} ${
              index < currentStep ? 'completed' : ''
            }`}
          >
            <span className="step-number">{index + 1}</span>
            <span className="step-title">{step.title}</span>
          </div>
        ))}
      </div>

      {/* Step content */}
      <div className="wizard-content">
        <h2>{currentStepConfig.title}</h2>
        <p className="step-description">{currentStepConfig.description}</p>

        {currentStepConfig.id === 'basic-info' && (
          <BasicInfoStep
            data={basicInfo}
            onChange={setBasicInfo}
            errors={errors}
          />
        )}

        {currentStepConfig.id === 'medical-history' && (
          <MedicalHistoryStep
            data={medicalHistory}
            onChange={setMedicalHistory}
          />
        )}

        {currentStepConfig.id === 'care-preferences' && (
          <CarePreferencesStep
            data={carePreferences}
            onChange={setCarePreferences}
            careLevels={config.careLevels}
            dietaryOptions={config.dietaryRequirements}
          />
        )}

        {currentStepConfig.id === 'emergency-contacts' && (
          <EmergencyContactsStep
            data={emergencyContacts}
            onChange={setEmergencyContacts}
            errors={errors}
          />
        )}

        {currentStepConfig.id === 'review' && (
          <ReviewStep
            basicInfo={basicInfo}
            medicalHistory={medicalHistory}
            carePreferences={carePreferences}
            emergencyContacts={emergencyContacts}
            consentGiven={consentGiven}
            termsAccepted={termsAccepted}
            onConsentChange={setConsentGiven}
            onTermsChange={setTermsAccepted}
            errors={errors}
          />
        )}
      </div>

      {/* Navigation */}
      <div className="wizard-actions">
        <button
          type="button"
          onClick={handleBack}
          disabled={currentStep === 0}
        >
          Back
        </button>
        <button
          type="button"
          onClick={handleNext}
          disabled={validateMutation.isPending || completeMutation.isPending}
        >
          {currentStep === config.steps.length - 1 ? 'Complete' : 'Next'}
        </button>
      </div>
    </div>
  );
}

// Step components
function BasicInfoStep({
  data,
  onChange,
  errors,
}: {
  data: BasicInfo;
  onChange: (data: BasicInfo) => void;
  errors: Record<string, string[]>;
}) {
  return (
    <div className="form-step">
      <div className="form-field">
        <label>First Name *</label>
        <input
          type="text"
          value={data.firstName}
          onChange={(e) => onChange({ ...data, firstName: e.target.value })}
        />
        {errors.firstName && <span className="error">{errors.firstName[0]}</span>}
      </div>
      <div className="form-field">
        <label>Last Name *</label>
        <input
          type="text"
          value={data.lastName}
          onChange={(e) => onChange({ ...data, lastName: e.target.value })}
        />
        {errors.lastName && <span className="error">{errors.lastName[0]}</span>}
      </div>
      <div className="form-field">
        <label>Date of Birth *</label>
        <input
          type="date"
          value={data.dateOfBirth}
          onChange={(e) => onChange({ ...data, dateOfBirth: e.target.value })}
        />
        {errors.dateOfBirth && <span className="error">{errors.dateOfBirth[0]}</span>}
      </div>
      <div className="form-field">
        <label>Room Preference</label>
        <input
          type="text"
          value={data.roomPreference}
          onChange={(e) => onChange({ ...data, roomPreference: e.target.value })}
          placeholder="e.g., Ground floor, Near nurse station"
        />
      </div>
    </div>
  );
}

function MedicalHistoryStep({
  data,
  onChange,
}: {
  data: MedicalHistory;
  onChange: (data: MedicalHistory) => void;
}) {
  return (
    <div className="form-step">
      <div className="form-field">
        <label>Medical Conditions</label>
        <textarea
          value={data.conditions.join('\n')}
          onChange={(e) =>
            onChange({ ...data, conditions: e.target.value.split('\n').filter(Boolean) })
          }
          placeholder="Enter each condition on a new line"
          rows={4}
        />
      </div>
      <div className="form-field">
        <label>Current Medications</label>
        <textarea
          value={data.medications.join('\n')}
          onChange={(e) =>
            onChange({ ...data, medications: e.target.value.split('\n').filter(Boolean) })
          }
          placeholder="Enter each medication on a new line"
          rows={4}
        />
      </div>
      <div className="form-field">
        <label>Allergies</label>
        <input
          type="text"
          value={data.allergies}
          onChange={(e) => onChange({ ...data, allergies: e.target.value })}
          placeholder="e.g., Penicillin, Shellfish"
        />
      </div>
      <div className="form-field">
        <label>Primary Care Physician</label>
        <input
          type="text"
          value={data.primaryCarePhysician}
          onChange={(e) => onChange({ ...data, primaryCarePhysician: e.target.value })}
        />
      </div>
    </div>
  );
}

function CarePreferencesStep({
  data,
  onChange,
  careLevels,
  dietaryOptions,
}: {
  data: CarePreferences;
  onChange: (data: CarePreferences) => void;
  careLevels: string[];
  dietaryOptions: string[];
}) {
  return (
    <div className="form-step">
      <div className="form-field">
        <label>Care Level *</label>
        <select
          value={data.careLevel}
          onChange={(e) => onChange({ ...data, careLevel: e.target.value })}
        >
          {careLevels.map((level) => (
            <option key={level} value={level}>
              {level}
            </option>
          ))}
        </select>
      </div>
      <div className="form-field">
        <label>Dietary Requirements</label>
        <div className="checkbox-group">
          {dietaryOptions.map((option) => (
            <label key={option} className="checkbox-label">
              <input
                type="checkbox"
                checked={data.dietaryRequirements.includes(option)}
                onChange={(e) => {
                  const newReqs = e.target.checked
                    ? [...data.dietaryRequirements, option]
                    : data.dietaryRequirements.filter((r) => r !== option);
                  onChange({ ...data, dietaryRequirements: newReqs });
                }}
              />
              {option}
            </label>
          ))}
        </div>
      </div>
      <div className="form-field">
        <label>Special Instructions</label>
        <textarea
          value={data.specialInstructions}
          onChange={(e) => onChange({ ...data, specialInstructions: e.target.value })}
          rows={3}
        />
      </div>
      <div className="form-field">
        <label className="checkbox-label">
          <input
            type="checkbox"
            checked={data.requiresNightChecks}
            onChange={(e) => onChange({ ...data, requiresNightChecks: e.target.checked })}
          />
          Requires Night Checks
        </label>
      </div>
    </div>
  );
}

function EmergencyContactsStep({
  data,
  onChange,
  errors,
}: {
  data: EmergencyContacts;
  onChange: (data: EmergencyContacts) => void;
  errors: Record<string, string[]>;
}) {
  return (
    <div className="form-step">
      <h3>Primary Contact</h3>
      <div className="form-field">
        <label>Name *</label>
        <input
          type="text"
          value={data.primaryContactName}
          onChange={(e) => onChange({ ...data, primaryContactName: e.target.value })}
        />
        {errors.primaryContactName && (
          <span className="error">{errors.primaryContactName[0]}</span>
        )}
      </div>
      <div className="form-field">
        <label>Phone *</label>
        <input
          type="tel"
          value={data.primaryContactPhone}
          onChange={(e) => onChange({ ...data, primaryContactPhone: e.target.value })}
        />
        {errors.primaryContactPhone && (
          <span className="error">{errors.primaryContactPhone[0]}</span>
        )}
      </div>
      <div className="form-field">
        <label>Relationship *</label>
        <input
          type="text"
          value={data.primaryContactRelationship}
          onChange={(e) => onChange({ ...data, primaryContactRelationship: e.target.value })}
        />
        {errors.primaryContactRelationship && (
          <span className="error">{errors.primaryContactRelationship[0]}</span>
        )}
      </div>

      <h3>Secondary Contact (Optional)</h3>
      <div className="form-field">
        <label>Name</label>
        <input
          type="text"
          value={data.secondaryContactName}
          onChange={(e) => onChange({ ...data, secondaryContactName: e.target.value })}
        />
      </div>
      <div className="form-field">
        <label>Phone</label>
        <input
          type="tel"
          value={data.secondaryContactPhone}
          onChange={(e) => onChange({ ...data, secondaryContactPhone: e.target.value })}
        />
      </div>
    </div>
  );
}

function ReviewStep({
  basicInfo,
  medicalHistory,
  carePreferences,
  emergencyContacts,
  consentGiven,
  termsAccepted,
  onConsentChange,
  onTermsChange,
  errors,
}: {
  basicInfo: BasicInfo;
  medicalHistory: MedicalHistory;
  carePreferences: CarePreferences;
  emergencyContacts: EmergencyContacts;
  consentGiven: boolean;
  termsAccepted: boolean;
  onConsentChange: (value: boolean) => void;
  onTermsChange: (value: boolean) => void;
  errors: Record<string, string[]>;
}) {
  return (
    <div className="review-step">
      <div className="review-section">
        <h3>Basic Information</h3>
        <p>
          <strong>Name:</strong> {basicInfo.firstName} {basicInfo.lastName}
        </p>
        <p>
          <strong>Date of Birth:</strong> {basicInfo.dateOfBirth}
        </p>
        {basicInfo.roomPreference && (
          <p>
            <strong>Room Preference:</strong> {basicInfo.roomPreference}
          </p>
        )}
      </div>

      <div className="review-section">
        <h3>Medical History</h3>
        <p>
          <strong>Conditions:</strong>{' '}
          {medicalHistory.conditions.length > 0
            ? medicalHistory.conditions.join(', ')
            : 'None specified'}
        </p>
        <p>
          <strong>Medications:</strong>{' '}
          {medicalHistory.medications.length > 0
            ? medicalHistory.medications.join(', ')
            : 'None'}
        </p>
        <p>
          <strong>Allergies:</strong> {medicalHistory.allergies || 'None'}
        </p>
      </div>

      <div className="review-section">
        <h3>Care Preferences</h3>
        <p>
          <strong>Care Level:</strong> {carePreferences.careLevel}
        </p>
        <p>
          <strong>Dietary Requirements:</strong>{' '}
          {carePreferences.dietaryRequirements.join(', ') || 'Regular'}
        </p>
        <p>
          <strong>Night Checks:</strong> {carePreferences.requiresNightChecks ? 'Yes' : 'No'}
        </p>
      </div>

      <div className="review-section">
        <h3>Emergency Contact</h3>
        <p>
          <strong>Primary:</strong> {emergencyContacts.primaryContactName} (
          {emergencyContacts.primaryContactRelationship}) - {emergencyContacts.primaryContactPhone}
        </p>
        {emergencyContacts.secondaryContactName && (
          <p>
            <strong>Secondary:</strong> {emergencyContacts.secondaryContactName} -{' '}
            {emergencyContacts.secondaryContactPhone}
          </p>
        )}
      </div>

      <div className="consent-section">
        <label className="checkbox-label">
          <input
            type="checkbox"
            checked={consentGiven}
            onChange={(e) => onConsentChange(e.target.checked)}
          />
          I consent to the collection and use of this information for care purposes
        </label>
        {errors.consentGiven && <span className="error">{errors.consentGiven[0]}</span>}

        <label className="checkbox-label">
          <input
            type="checkbox"
            checked={termsAccepted}
            onChange={(e) => onTermsChange(e.target.checked)}
          />
          I accept the terms and conditions
        </label>
        {errors.termsAccepted && <span className="error">{errors.termsAccepted[0]}</span>}
      </div>
    </div>
  );
}
