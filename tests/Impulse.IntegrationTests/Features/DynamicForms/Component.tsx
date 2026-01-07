import * as React from 'react';
import { useMutation } from '../Shared/runtime';
import type {
  DynamicFormProps,
  FormSection,
  FormField,
  FormValues,
  VisibilityCondition,
  SelectOption,
  FieldConstraints,
  FormSubmitRequest,
  FormSubmitResponse,
} from '../Shared/types.g';

// ============================================================================
// Main Dynamic Form Component
// ============================================================================

export function DynamicForm({
  formTitle,
  formDescription,
  sections,
  values,
  validationErrors,
  isSubmitting,
}: DynamicFormProps) {
  const [localValues, setLocalValues] = React.useState<FormValues>(values);
  const [localErrors, setLocalErrors] = React.useState(validationErrors);

  const submitMutation = useMutation<FormSubmitRequest, FormSubmitResponse>('/forms/submit', {
    onSuccess: (response) => {
      if (response.redirectUrl) {
        window.location.href = response.redirectUrl;
      }
    },
    onError: (error) => {
      if ('validationErrors' in error && error.validationErrors) {
        setLocalErrors(error.validationErrors as Record<string, string[]>);
      }
    },
  });

  const handleSubmit = (e: React.FormEvent) => {
    e.preventDefault();
    submitMutation.mutate({
      formId: 'dynamic-form',
      values: localValues,
    });
  };

  const updateValue = (fieldName: string, value: unknown, fieldType: string) => {
    setLocalValues((prev) => {
      switch (fieldType) {
        case 'Number':
        case 'Currency':
          return {
            ...prev,
            numberValues: { ...prev.numberValues, [fieldName]: value as number },
          };
        case 'Checkbox':
          return {
            ...prev,
            booleanValues: { ...prev.booleanValues, [fieldName]: value as boolean },
          };
        case 'MultiSelect':
          return {
            ...prev,
            multiSelectValues: { ...prev.multiSelectValues, [fieldName]: value as string[] },
          };
        case 'Date':
        case 'DateTime':
          // Dates are serialized as ISO strings
          return {
            ...prev,
            dateValues: { ...prev.dateValues, [fieldName]: value as string },
          };
        default:
          return {
            ...prev,
            stringValues: { ...prev.stringValues, [fieldName]: value as string },
          };
      }
    });
    // Clear field error when user changes value
    if (localErrors[fieldName]) {
      setLocalErrors((prev) => {
        const { [fieldName]: _, ...rest } = prev;
        return rest;
      });
    }
  };

  const getFieldValue = (fieldName: string, fieldType: string): unknown => {
    switch (fieldType) {
      case 'Number':
      case 'Currency':
        return localValues.numberValues[fieldName];
      case 'Checkbox':
        return localValues.booleanValues[fieldName] ?? false;
      case 'MultiSelect':
        return localValues.multiSelectValues[fieldName] ?? [];
      case 'Date':
      case 'DateTime':
        return localValues.dateValues[fieldName];
      default:
        return localValues.stringValues[fieldName] ?? '';
    }
  };

  const loading = isSubmitting || submitMutation.state.status === 'loading';

  return (
    <form className="dynamic-form" onSubmit={handleSubmit}>
      <div className="form-header">
        <h1>{formTitle}</h1>
        {formDescription && <p className="form-description">{formDescription}</p>}
      </div>

      {sections.map((section) => (
        <DynamicSection
          key={section.id}
          section={section}
          values={localValues}
          errors={localErrors}
          onFieldChange={updateValue}
          getFieldValue={getFieldValue}
        />
      ))}

      <div className="form-actions">
        <button type="submit" disabled={loading} className="btn-primary">
          {loading ? 'Submitting...' : 'Submit'}
        </button>
      </div>
    </form>
  );
}

// ============================================================================
// Section Component with Visibility Handling
// ============================================================================

interface SectionProps {
  section: FormSection;
  values: FormValues;
  errors: Record<string, readonly string[]>;
  onFieldChange: (name: string, value: unknown, type: string) => void;
  getFieldValue: (name: string, type: string) => unknown;
}

function DynamicSection({ section, values, errors, onFieldChange, getFieldValue }: SectionProps) {
  // Check section visibility
  if (section.visibleWhen && !evaluateCondition(section.visibleWhen, values)) {
    return null;
  }

  return (
    <fieldset className="form-section">
      <legend>{section.title}</legend>
      {section.description && <p className="section-description">{section.description}</p>}

      <div className="section-fields">
        {section.fields.map((field) => (
          <DynamicField
            key={field.name}
            field={field}
            values={values}
            errors={errors[field.name]}
            onChange={(value) => onFieldChange(field.name, value, field.type)}
            value={getFieldValue(field.name, field.type)}
          />
        ))}
      </div>
    </fieldset>
  );
}

// ============================================================================
// Field Component with Visibility and Type Handling
// ============================================================================

interface FieldProps {
  field: FormField;
  values: FormValues;
  errors?: readonly string[];
  onChange: (value: unknown) => void;
  value: unknown;
}

function DynamicField({ field, values, errors, onChange, value }: FieldProps) {
  // Check field visibility
  if (field.visibleWhen && !evaluateCondition(field.visibleWhen, values)) {
    return null;
  }

  const hasError = errors && errors.length > 0;

  return (
    <div className={`form-field ${hasError ? 'has-error' : ''} field-${field.type.toLowerCase()}`}>
      <label htmlFor={field.name}>
        {field.label}
        {field.required && <span className="required">*</span>}
      </label>

      <FieldInput
        field={field}
        value={value}
        onChange={onChange}
        hasError={hasError ?? false}
      />

      {field.helpText && <span className="help-text">{field.helpText}</span>}

      {errors?.map((error, i) => (
        <span key={i} className="error-message">
          {error}
        </span>
      ))}
    </div>
  );
}

// ============================================================================
// Field Input Renderer - Handles Different Field Types
// ============================================================================

interface FieldInputProps {
  field: FormField;
  value: unknown;
  onChange: (value: unknown) => void;
  hasError: boolean;
}

function FieldInput({ field, value, onChange, hasError }: FieldInputProps) {
  const commonProps = {
    id: field.name,
    name: field.name,
    required: field.required,
    placeholder: field.placeholder ?? undefined,
    'aria-invalid': hasError,
  };

  const constraints = field.constraints;

  switch (field.type) {
    case 'Text':
    case 'Email':
    case 'Phone':
      return (
        <input
          {...commonProps}
          type={field.type === 'Email' ? 'email' : field.type === 'Phone' ? 'tel' : 'text'}
          value={(value as string) ?? ''}
          onChange={(e) => onChange(e.target.value)}
          minLength={constraints?.minLength ?? undefined}
          maxLength={constraints?.maxLength ?? undefined}
          pattern={constraints?.pattern ?? undefined}
        />
      );

    case 'Number':
    case 'Currency':
      return (
        <input
          {...commonProps}
          type="number"
          value={(value as number) ?? ''}
          onChange={(e) => onChange(e.target.value ? parseFloat(e.target.value) : undefined)}
          min={constraints?.min != null ? Number(constraints.min) : undefined}
          max={constraints?.max != null ? Number(constraints.max) : undefined}
          step={field.type === 'Currency' ? '0.01' : undefined}
        />
      );

    case 'Date':
      return (
        <input
          {...commonProps}
          type="date"
          value={formatDateValue(value)}
          onChange={(e) => onChange(e.target.value)}
        />
      );

    case 'DateTime':
      return (
        <input
          {...commonProps}
          type="datetime-local"
          value={formatDateTimeValue(value)}
          onChange={(e) => onChange(e.target.value)}
        />
      );

    case 'Select':
      return (
        <select
          {...commonProps}
          value={(value as string) ?? ''}
          onChange={(e) => onChange(e.target.value)}
        >
          <option value="">Select...</option>
          {field.options?.map((opt) => (
            <SelectOptionItem key={opt.value} option={opt} />
          ))}
        </select>
      );

    case 'MultiSelect':
      return (
        <MultiSelectInput
          field={field}
          value={(value as string[]) ?? []}
          onChange={onChange}
        />
      );

    case 'Radio':
      return (
        <div className="radio-group" role="radiogroup" aria-labelledby={`${field.name}-label`}>
          {field.options?.map((opt) => (
            <label key={opt.value} className="radio-option">
              <input
                type="radio"
                name={field.name}
                value={opt.value}
                checked={value === opt.value}
                onChange={(e) => onChange(e.target.value)}
                disabled={opt.disabled ?? false}
              />
              {opt.label}
            </label>
          ))}
        </div>
      );

    case 'Checkbox':
      return (
        <input
          {...commonProps}
          type="checkbox"
          checked={(value as boolean) ?? false}
          onChange={(e) => onChange(e.target.checked)}
        />
      );

    case 'Textarea':
      return (
        <textarea
          {...commonProps}
          value={(value as string) ?? ''}
          onChange={(e) => onChange(e.target.value)}
          minLength={constraints?.minLength ?? undefined}
          maxLength={constraints?.maxLength ?? undefined}
          rows={5}
        />
      );

    case 'Hidden':
      return <input type="hidden" name={field.name} value={(value as string) ?? ''} />;

    case 'File':
      return (
        <input
          {...commonProps}
          type="file"
          onChange={(e) => {
            const file = e.target.files?.[0];
            if (file) {
              onChange(file.name);
            }
          }}
        />
      );

    default:
      return (
        <input
          {...commonProps}
          type="text"
          value={(value as string) ?? ''}
          onChange={(e) => onChange(e.target.value)}
        />
      );
  }
}

// ============================================================================
// Multi-Select Component
// ============================================================================

interface MultiSelectProps {
  field: FormField;
  value: string[];
  onChange: (value: string[]) => void;
}

function MultiSelectInput({ field, value, onChange }: MultiSelectProps) {
  const toggleOption = (optionValue: string) => {
    if (value.includes(optionValue)) {
      onChange(value.filter((v) => v !== optionValue));
    } else {
      onChange([...value, optionValue]);
    }
  };

  return (
    <div className="multiselect-group">
      {field.options?.map((opt) => (
        <label key={opt.value} className="multiselect-option">
          <input
            type="checkbox"
            checked={value.includes(opt.value)}
            onChange={() => toggleOption(opt.value)}
            disabled={opt.disabled ?? false}
          />
          {opt.label}
        </label>
      ))}
    </div>
  );
}

function SelectOptionItem({ option }: { option: SelectOption }) {
  return (
    <option value={option.value} disabled={option.disabled ?? false}>
      {option.label}
    </option>
  );
}

// ============================================================================
// Conditional Visibility Evaluation
// ============================================================================

function evaluateCondition(condition: VisibilityCondition, values: FormValues): boolean {
  // Handle compound AND conditions
  if (condition.and && condition.and.length > 0) {
    return condition.and.every((c) => evaluateCondition(c, values));
  }

  // Handle compound OR conditions
  if (condition.or && condition.or.length > 0) {
    return condition.or.some((c) => evaluateCondition(c, values));
  }

  // Handle simple field condition
  if (!condition.field || !condition.operator) {
    return true; // No condition = always visible
  }

  const fieldValue = getValueFromFormValues(condition.field, values);

  switch (condition.operator) {
    case 'Equals':
      return fieldValue === condition.value;

    case 'NotEquals':
      return fieldValue !== condition.value;

    case 'Contains':
      if (typeof fieldValue === 'string') {
        return fieldValue.includes(String(condition.value));
      }
      if (Array.isArray(fieldValue)) {
        return fieldValue.includes(condition.value as string);
      }
      return false;

    case 'NotContains':
      if (typeof fieldValue === 'string') {
        return !fieldValue.includes(String(condition.value));
      }
      if (Array.isArray(fieldValue)) {
        return !fieldValue.includes(condition.value as string);
      }
      return true;

    case 'GreaterThan':
      return typeof fieldValue === 'number' && fieldValue > (condition.value as number);

    case 'LessThan':
      return typeof fieldValue === 'number' && fieldValue < (condition.value as number);

    case 'IsEmpty':
      return fieldValue === null || fieldValue === undefined || fieldValue === '' ||
        (Array.isArray(fieldValue) && fieldValue.length === 0);

    case 'IsNotEmpty':
      return fieldValue !== null && fieldValue !== undefined && fieldValue !== '' &&
        (!Array.isArray(fieldValue) || fieldValue.length > 0);

    default:
      return true;
  }
}

function getValueFromFormValues(fieldName: string, values: FormValues): unknown {
  // Check all value stores
  if (fieldName in values.stringValues) {
    return values.stringValues[fieldName];
  }
  if (fieldName in values.numberValues) {
    return values.numberValues[fieldName];
  }
  if (fieldName in values.booleanValues) {
    return values.booleanValues[fieldName];
  }
  if (fieldName in values.multiSelectValues) {
    return values.multiSelectValues[fieldName];
  }
  if (fieldName in values.dateValues) {
    return values.dateValues[fieldName];
  }
  return undefined;
}

// ============================================================================
// Date Formatting Helpers
// ============================================================================

function formatDateValue(value: unknown): string {
  if (!value) return '';
  if (typeof value === 'string') {
    // If already ISO string, extract date part
    return value.split('T')[0];
  }
  if (value instanceof Date) {
    return value.toISOString().split('T')[0];
  }
  return '';
}

function formatDateTimeValue(value: unknown): string {
  if (!value) return '';
  if (typeof value === 'string') {
    // Convert to datetime-local format (YYYY-MM-DDTHH:mm)
    const date = new Date(value);
    if (isNaN(date.getTime())) return value;
    return date.toISOString().slice(0, 16);
  }
  if (value instanceof Date) {
    return value.toISOString().slice(0, 16);
  }
  return '';
}

// ============================================================================
// Insurance Application Form (Example Implementation)
// ============================================================================

export function InsuranceApplicationForm({
  applicantName,
  formData,
  schema,
  errors,
}: {
  applicantName: string;
  formData: Record<string, unknown>;
  schema: { sections: readonly FormSection[] };
  errors: Record<string, readonly string[]>;
}) {
  const [localData, setLocalData] = React.useState(formData);
  const [localErrors, setLocalErrors] = React.useState(errors);

  // Convert Record to FormValues structure for condition evaluation
  // Note: Dates are already strings (ISO format) from JSON serialization
  const formValues: FormValues = React.useMemo(() => ({
    stringValues: Object.fromEntries(
      Object.entries(localData).filter(([, v]) => typeof v === 'string')
    ) as Record<string, string>,
    numberValues: Object.fromEntries(
      Object.entries(localData).filter(([, v]) => typeof v === 'number')
    ) as Record<string, number>,
    booleanValues: Object.fromEntries(
      Object.entries(localData).filter(([, v]) => typeof v === 'boolean')
    ) as Record<string, boolean>,
    multiSelectValues: Object.fromEntries(
      Object.entries(localData).filter(([, v]) => Array.isArray(v))
    ) as Record<string, string[]>,
    dateValues: {} as Record<string, string>, // Date fields are in stringValues as ISO strings
  }), [localData]);

  const updateField = (name: string, value: unknown) => {
    setLocalData((prev) => ({ ...prev, [name]: value }));
    if (localErrors[name]) {
      setLocalErrors((prev) => {
        const { [name]: _, ...rest } = prev;
        return rest;
      });
    }
  };

  return (
    <form className="insurance-form">
      <div className="form-header">
        <h1>Insurance Application</h1>
        <p>Welcome, {applicantName}. Please complete the form below.</p>
      </div>

      {schema.sections.map((section) => {
        // Check section visibility
        if (section.visibleWhen && !evaluateCondition(section.visibleWhen, formValues)) {
          return null;
        }

        return (
          <fieldset key={section.id} className="form-section">
            <legend>{section.title}</legend>
            {section.description && <p>{section.description}</p>}

            <div className="section-fields">
              {section.fields.map((field) => {
                // Check field visibility
                if (field.visibleWhen && !evaluateCondition(field.visibleWhen, formValues)) {
                  return null;
                }

                return (
                  <DynamicField
                    key={field.name}
                    field={field}
                    values={formValues}
                    errors={localErrors[field.name]}
                    value={localData[field.name]}
                    onChange={(value) => updateField(field.name, value)}
                  />
                );
              })}
            </div>
          </fieldset>
        );
      })}

      <div className="form-actions">
        <button type="submit" className="btn-primary">
          Submit Application
        </button>
      </div>
    </form>
  );
}
