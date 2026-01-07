import * as React from 'react';
import { useMutation } from '../Shared/runtime';
import type { WizardProps, WizardFormData, WizardStepData } from '../Shared/types.g';

// Request/Response types for wizard mutations
interface WizardStepRequest {
  currentStep: number;
  formData: WizardFormData;
}

interface WizardStepResponse {
  success: boolean;
  nextStep: number;
  validationErrors?: Record<string, string[]>;
}

interface WizardBackRequest {
  currentStep: number;
}

interface WizardSubmitRequest {
  formData: WizardFormData;
}

interface WizardSubmitResponse {
  success: boolean;
  residentId?: number;
  message?: string;
}

export function Wizard({ currentStep, totalSteps, steps, formData, canGoBack, canGoNext, canSubmit }: WizardProps) {
  const [localFormData, setLocalFormData] = React.useState<WizardFormData>(formData);

  const nextMutation = useMutation<WizardStepRequest, WizardStepResponse>('/wizard/next', {
    onSuccess: () => window.location.reload(),
  });

  const backMutation = useMutation<WizardBackRequest, WizardStepResponse>('/wizard/back', {
    onSuccess: () => window.location.reload(),
  });

  const submitMutation = useMutation<WizardSubmitRequest, WizardSubmitResponse>('/wizard/submit', {
    onSuccess: (data) => {
      if (data.residentId) {
        window.location.href = `/residents/${data.residentId}`;
      }
    },
  });

  const handleNext = () => {
    nextMutation.mutate({ currentStep, formData: localFormData });
  };

  const handleBack = () => {
    backMutation.mutate({ currentStep });
  };

  const handleSubmit = () => {
    submitMutation.mutate({ formData: localFormData });
  };

  const updateField = <K extends keyof WizardFormData>(field: K, value: WizardFormData[K]) => {
    setLocalFormData((prev) => ({ ...prev, [field]: value }));
  };

  // Extract validation errors from mutation state
  const validationErrors =
    nextMutation.state.status === 'error' && 'validationErrors' in nextMutation.state
      ? nextMutation.state.validationErrors || {}
      : {};

  const isLoading =
    nextMutation.state.status === 'loading' ||
    backMutation.state.status === 'loading' ||
    submitMutation.state.status === 'loading';

  return (
    <div className="wizard">
      <WizardProgress steps={steps} />

      <div className="wizard-content">
        <WizardStepHeader step={steps.find((s) => s.isCurrent)} />

        {currentStep === 1 && (
          <PersonalInfoStep
            formData={localFormData}
            updateField={updateField}
            errors={validationErrors}
          />
        )}

        {currentStep === 2 && (
          <MedicalHistoryStep
            formData={localFormData}
            updateField={updateField}
            errors={validationErrors}
          />
        )}

        {currentStep === 3 && (
          <EmergencyContactStep
            formData={localFormData}
            updateField={updateField}
            errors={validationErrors}
          />
        )}

        {currentStep === 4 && (
          <RoomAssignmentStep
            formData={localFormData}
            updateField={updateField}
            errors={validationErrors}
          />
        )}
      </div>

      <WizardNavigation
        canGoBack={canGoBack}
        canGoNext={canGoNext}
        canSubmit={canSubmit}
        isLoading={isLoading}
        onBack={handleBack}
        onNext={handleNext}
        onSubmit={handleSubmit}
      />
    </div>
  );
}

// Sub-components

function WizardProgress({ steps }: { steps: readonly WizardStepData[] }) {
  return (
    <div className="wizard-progress">
      {steps.map((step) => (
        <div
          key={step.stepNumber}
          className={`wizard-step ${step.isCompleted ? 'completed' : ''} ${step.isCurrent ? 'current' : ''}`}
        >
          <div className="step-number">{step.stepNumber}</div>
          <div className="step-title">{step.title}</div>
        </div>
      ))}
    </div>
  );
}

function WizardStepHeader({ step }: { step?: WizardStepData }) {
  if (!step) return null;
  return (
    <div className="wizard-step-header">
      <h2>{step.title}</h2>
      <p>{step.description}</p>
    </div>
  );
}

interface StepProps {
  formData: WizardFormData;
  updateField: <K extends keyof WizardFormData>(field: K, value: WizardFormData[K]) => void;
  errors: Record<string, string[]>;
}

function PersonalInfoStep({ formData, updateField, errors }: StepProps) {
  return (
    <div className="wizard-step-content">
      <FormField
        label="First Name"
        name="firstName"
        value={formData.firstName || ''}
        onChange={(v) => updateField('firstName', v)}
        errors={errors.firstName}
        required
      />
      <FormField
        label="Last Name"
        name="lastName"
        value={formData.lastName || ''}
        onChange={(v) => updateField('lastName', v)}
        errors={errors.lastName}
        required
      />
      <FormField
        label="Date of Birth"
        name="dateOfBirth"
        type="date"
        value={formData.dateOfBirth || ''}
        onChange={(v) => updateField('dateOfBirth', v || null)}
        errors={errors.dateOfBirth}
        required
      />
      <FormField
        label="Gender"
        name="gender"
        value={formData.gender || ''}
        onChange={(v) => updateField('gender', v)}
        errors={errors.gender}
      />
    </div>
  );
}

function MedicalHistoryStep({ formData, updateField, errors }: StepProps) {
  return (
    <div className="wizard-step-content">
      <FormField
        label="Primary Physician"
        name="primaryPhysician"
        value={formData.primaryPhysician || ''}
        onChange={(v) => updateField('primaryPhysician', v)}
        errors={errors.primaryPhysician}
        required
      />
      <FormField
        label="Blood Type"
        name="bloodType"
        value={formData.bloodType || ''}
        onChange={(v) => updateField('bloodType', v)}
        errors={errors.bloodType}
      />
      <FormCheckbox
        label="Has Insurance"
        name="hasInsurance"
        checked={formData.hasInsurance}
        onChange={(v) => updateField('hasInsurance', v)}
      />
      {/* Conditional fields based on hasInsurance */}
      {formData.hasInsurance && (
        <>
          <FormField
            label="Insurance Provider"
            name="insuranceProvider"
            value={formData.insuranceProvider || ''}
            onChange={(v) => updateField('insuranceProvider', v)}
            errors={errors.insuranceProvider}
            required
          />
          <FormField
            label="Policy Number"
            name="insurancePolicyNumber"
            value={formData.insurancePolicyNumber || ''}
            onChange={(v) => updateField('insurancePolicyNumber', v)}
            errors={errors.insurancePolicyNumber}
            required
          />
        </>
      )}
    </div>
  );
}

function EmergencyContactStep({ formData, updateField, errors }: StepProps) {
  return (
    <div className="wizard-step-content">
      <FormField
        label="Contact Name"
        name="emergencyContactName"
        value={formData.emergencyContactName || ''}
        onChange={(v) => updateField('emergencyContactName', v)}
        errors={errors.emergencyContactName}
        required
      />
      <FormField
        label="Phone"
        name="emergencyContactPhone"
        value={formData.emergencyContactPhone || ''}
        onChange={(v) => updateField('emergencyContactPhone', v)}
        errors={errors.emergencyContactPhone}
        required
      />
      <FormField
        label="Relationship"
        name="emergencyContactRelation"
        value={formData.emergencyContactRelation || ''}
        onChange={(v) => updateField('emergencyContactRelation', v)}
        errors={errors.emergencyContactRelation}
      />
    </div>
  );
}

function RoomAssignmentStep({ formData, updateField, errors }: StepProps) {
  return (
    <div className="wizard-step-content">
      <FormField
        label="Building ID"
        name="buildingId"
        type="number"
        value={formData.buildingId?.toString() || ''}
        onChange={(v) => updateField('buildingId', v ? parseInt(v, 10) : null)}
        errors={errors.buildingId}
        required
      />
      <FormField
        label="Floor Number"
        name="floorNumber"
        type="number"
        value={formData.floorNumber?.toString() || ''}
        onChange={(v) => updateField('floorNumber', v ? parseInt(v, 10) : null)}
        errors={errors.floorNumber}
      />
      <FormField
        label="Room Number"
        name="roomNumber"
        type="number"
        value={formData.roomNumber?.toString() || ''}
        onChange={(v) => updateField('roomNumber', v ? parseInt(v, 10) : null)}
        errors={errors.roomNumber}
        required
      />
    </div>
  );
}

// Reusable form components

interface FormFieldProps {
  label: string;
  name: string;
  value: string;
  onChange: (value: string) => void;
  type?: 'text' | 'date' | 'number';
  errors?: string[];
  required?: boolean;
}

function FormField({ label, name, value, onChange, type = 'text', errors, required }: FormFieldProps) {
  return (
    <div className={`form-field ${errors?.length ? 'has-error' : ''}`}>
      <label htmlFor={name}>
        {label}
        {required && <span className="required">*</span>}
      </label>
      <input
        id={name}
        name={name}
        type={type}
        value={value}
        onChange={(e) => onChange(e.target.value)}
        required={required}
      />
      {errors?.map((error, i) => (
        <div key={i} className="error-message">
          {error}
        </div>
      ))}
    </div>
  );
}

interface FormCheckboxProps {
  label: string;
  name: string;
  checked: boolean;
  onChange: (value: boolean) => void;
}

function FormCheckbox({ label, name, checked, onChange }: FormCheckboxProps) {
  return (
    <div className="form-checkbox">
      <label>
        <input
          type="checkbox"
          name={name}
          checked={checked}
          onChange={(e) => onChange(e.target.checked)}
        />
        {label}
      </label>
    </div>
  );
}

interface WizardNavigationProps {
  canGoBack: boolean;
  canGoNext: boolean;
  canSubmit: boolean;
  isLoading: boolean;
  onBack: () => void;
  onNext: () => void;
  onSubmit: () => void;
}

function WizardNavigation({
  canGoBack,
  canGoNext,
  canSubmit,
  isLoading,
  onBack,
  onNext,
  onSubmit,
}: WizardNavigationProps) {
  return (
    <div className="wizard-navigation">
      <button
        type="button"
        onClick={onBack}
        disabled={!canGoBack || isLoading}
        className="btn-secondary"
      >
        Back
      </button>

      {canSubmit ? (
        <button
          type="button"
          onClick={onSubmit}
          disabled={isLoading}
          className="btn-primary"
        >
          {isLoading ? 'Submitting...' : 'Submit'}
        </button>
      ) : (
        <button
          type="button"
          onClick={onNext}
          disabled={!canGoNext || isLoading}
          className="btn-primary"
        >
          {isLoading ? 'Validating...' : 'Next'}
        </button>
      )}
    </div>
  );
}
