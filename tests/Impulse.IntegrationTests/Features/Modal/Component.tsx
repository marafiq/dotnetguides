import * as React from 'react';
import { useMutation, navigate } from '../Shared/runtime';
import type {
  ModalContainerProps,
  ModalConfig,
  ModalContent,
  ModalAction,
  ModalFormField,
  ModalSubmitRequest,
  ModalSubmitResponse,
  DeleteConfirmationProps,
  DeleteRequest,
  DeleteResponse,
  EditResidentModalProps,
  UpdateResidentRequest,
  UpdateResidentResponse,
} from '../Shared/types.g';

// ============================================================================
// Modal Container - Manages modal state and rendering
// ============================================================================

export function ModalContainer({ isOpen, modal, returnUrl }: ModalContainerProps) {
  const [formData, setFormData] = React.useState<Record<string, unknown>>({});
  const [localErrors, setLocalErrors] = React.useState<Record<string, string[]>>({});

  // Initialize form data from modal content
  React.useEffect(() => {
    if (modal?.content.formValues) {
      setFormData(modal.content.formValues as Record<string, unknown>);
    } else if (modal?.content.formFields) {
      const initial: Record<string, unknown> = {};
      for (const field of modal.content.formFields) {
        initial[field.name] = field.defaultValue ?? '';
      }
      setFormData(initial);
    }
  }, [modal]);

  const submitMutation = useMutation<ModalSubmitRequest, ModalSubmitResponse>('/modal/submit', {
    onSuccess: (response) => {
      if (response.redirectUrl) {
        window.location.href = response.redirectUrl;
      } else if (response.nextModal) {
        // Server returned a new modal to show
        window.location.reload();
      } else if (returnUrl) {
        window.location.href = returnUrl;
      }
    },
    onError: (error) => {
      if ('validationErrors' in error && error.validationErrors) {
        setLocalErrors(error.validationErrors as Record<string, string[]>);
      }
    },
  });

  const handleClose = () => {
    if (returnUrl) {
      navigate(returnUrl);
    }
  };

  const handleAction = (action: ModalAction) => {
    switch (action.type) {
      case 'Close':
        handleClose();
        break;
      case 'Submit':
        if (action.confirmMessage && !window.confirm(action.confirmMessage)) {
          return;
        }
        submitMutation.mutate({
          modalId: modal?.id ?? '',
          actionId: action.id,
          formData: formData,
        });
        break;
      case 'Navigate':
        if (action.endpoint) {
          navigate(action.endpoint);
        }
        break;
      case 'Custom':
        // Custom actions handled by parent component
        break;
    }
  };

  const updateFormField = (name: string, value: unknown) => {
    setFormData((prev) => ({ ...prev, [name]: value }));
    if (localErrors[name]) {
      setLocalErrors((prev) => {
        const { [name]: _, ...rest } = prev;
        return rest;
      });
    }
  };

  if (!isOpen || !modal) {
    return null;
  }

  const isLoading = submitMutation.state.status === 'loading';

  return (
    <Modal
      config={modal}
      onClose={handleClose}
      onAction={handleAction}
      formData={formData}
      onFieldChange={updateFormField}
      errors={localErrors}
      isLoading={isLoading}
    />
  );
}

// ============================================================================
// Base Modal Component
// ============================================================================

interface ModalProps {
  config: ModalConfig;
  onClose: () => void;
  onAction: (action: ModalAction) => void;
  formData: Record<string, unknown>;
  onFieldChange: (name: string, value: unknown) => void;
  errors: Record<string, string[]>;
  isLoading: boolean;
}

function Modal({ config, onClose, onAction, formData, onFieldChange, errors, isLoading }: ModalProps) {
  const modalRef = React.useRef<HTMLDivElement>(null);

  // Handle escape key
  React.useEffect(() => {
    if (!config.closeOnEscape) return;

    const handleEscape = (e: KeyboardEvent) => {
      if (e.key === 'Escape') {
        onClose();
      }
    };

    document.addEventListener('keydown', handleEscape);
    return () => document.removeEventListener('keydown', handleEscape);
  }, [config.closeOnEscape, onClose]);

  // Focus trap
  React.useEffect(() => {
    const modal = modalRef.current;
    if (!modal) return;

    const focusableElements = modal.querySelectorAll<HTMLElement>(
      'button, [href], input, select, textarea, [tabindex]:not([tabindex="-1"])'
    );
    const firstElement = focusableElements[0];
    const lastElement = focusableElements[focusableElements.length - 1];

    firstElement?.focus();

    const handleTab = (e: KeyboardEvent) => {
      if (e.key !== 'Tab') return;

      if (e.shiftKey) {
        if (document.activeElement === firstElement) {
          e.preventDefault();
          lastElement?.focus();
        }
      } else {
        if (document.activeElement === lastElement) {
          e.preventDefault();
          firstElement?.focus();
        }
      }
    };

    document.addEventListener('keydown', handleTab);
    return () => document.removeEventListener('keydown', handleTab);
  }, []);

  const handleOverlayClick = (e: React.MouseEvent) => {
    if (config.closeOnOverlay && e.target === e.currentTarget) {
      onClose();
    }
  };

  const sizeClass = {
    Small: 'modal-sm',
    Medium: 'modal-md',
    Large: 'modal-lg',
    FullScreen: 'modal-fullscreen',
  }[config.size];

  return (
    <div className="modal-overlay" onClick={handleOverlayClick} role="dialog" aria-modal="true" aria-labelledby="modal-title">
      <div ref={modalRef} className={`modal ${sizeClass}`}>
        <div className="modal-header">
          <h2 id="modal-title">{config.title}</h2>
          {config.showCloseButton && (
            <button type="button" className="modal-close" onClick={onClose} aria-label="Close">
              ×
            </button>
          )}
        </div>

        {config.description && <p className="modal-description">{config.description}</p>}

        <div className="modal-body">
          <ModalContentRenderer
            content={config.content}
            formData={formData}
            onFieldChange={onFieldChange}
            errors={errors}
          />
        </div>

        <div className="modal-footer">
          {config.actions.map((action) => (
            <ModalButton
              key={action.id}
              action={action}
              onClick={() => onAction(action)}
              isLoading={isLoading && action.type === 'Submit'}
            />
          ))}
        </div>
      </div>
    </div>
  );
}

// ============================================================================
// Modal Content Renderer
// ============================================================================

interface ContentProps {
  content: ModalContent;
  formData: Record<string, unknown>;
  onFieldChange: (name: string, value: unknown) => void;
  errors: Record<string, string[]>;
}

function ModalContentRenderer({ content, formData, onFieldChange, errors }: ContentProps) {
  switch (content.type) {
    case 'Message':
      return <p className="modal-message">{content.message}</p>;

    case 'Alert':
      return (
        <div className={`modal-alert alert-${content.alertSeverity?.toLowerCase()}`}>
          <AlertIcon severity={content.alertSeverity ?? 'Info'} />
          <p>{content.message}</p>
        </div>
      );

    case 'Form':
      return (
        <form className="modal-form" onSubmit={(e) => e.preventDefault()}>
          {content.formFields?.map((field) => (
            <ModalFormFieldRenderer
              key={field.name}
              field={field}
              value={formData[field.name]}
              onChange={(value) => onFieldChange(field.name, value)}
              errors={errors[field.name]}
            />
          ))}
        </form>
      );

    case 'Component':
      // Custom component would be rendered by component registry
      return <div className="modal-custom" data-component={content.componentPath} />;

    default:
      return null;
  }
}

// ============================================================================
// Modal Form Field Renderer
// ============================================================================

interface FieldRendererProps {
  field: ModalFormField;
  value: unknown;
  onChange: (value: unknown) => void;
  errors?: string[];
}

function ModalFormFieldRenderer({ field, value, onChange, errors }: FieldRendererProps) {
  const hasError = errors && errors.length > 0;
  const inputId = `modal-field-${field.name}`;

  return (
    <div className={`form-field ${hasError ? 'has-error' : ''}`}>
      <label htmlFor={inputId}>
        {field.label}
        {field.required && <span className="required">*</span>}
      </label>

      {renderInput(field, inputId, value, onChange)}

      {field.validationMessage && !hasError && (
        <span className="help-text">{field.validationMessage}</span>
      )}

      {errors?.map((error, i) => (
        <span key={i} className="error-message">
          {error}
        </span>
      ))}
    </div>
  );
}

function renderInput(
  field: ModalFormField,
  id: string,
  value: unknown,
  onChange: (value: unknown) => void
) {
  const commonProps = {
    id,
    name: field.name,
    required: field.required,
    placeholder: field.placeholder ?? undefined,
  };

  switch (field.type) {
    case 'Text':
      return (
        <input
          {...commonProps}
          type="text"
          value={(value as string) ?? ''}
          onChange={(e) => onChange(e.target.value)}
        />
      );

    case 'Email':
      return (
        <input
          {...commonProps}
          type="email"
          value={(value as string) ?? ''}
          onChange={(e) => onChange(e.target.value)}
        />
      );

    case 'Password':
      return (
        <input
          {...commonProps}
          type="password"
          value={(value as string) ?? ''}
          onChange={(e) => onChange(e.target.value)}
        />
      );

    case 'Number':
      return (
        <input
          {...commonProps}
          type="number"
          value={(value as number) ?? ''}
          onChange={(e) => onChange(e.target.value ? parseFloat(e.target.value) : undefined)}
        />
      );

    case 'Textarea':
      return (
        <textarea
          {...commonProps}
          value={(value as string) ?? ''}
          onChange={(e) => onChange(e.target.value)}
          rows={4}
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
            <option key={opt.value} value={opt.value}>
              {opt.label}
            </option>
          ))}
        </select>
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

    case 'Date':
      return (
        <input
          {...commonProps}
          type="date"
          value={(value as string) ?? ''}
          onChange={(e) => onChange(e.target.value)}
        />
      );

    default:
      return null;
  }
}

// ============================================================================
// Modal Button
// ============================================================================

interface ButtonProps {
  action: ModalAction;
  onClick: () => void;
  isLoading: boolean;
}

function ModalButton({ action, onClick, isLoading }: ButtonProps) {
  const styleClass = {
    Primary: 'btn-primary',
    Secondary: 'btn-secondary',
    Danger: 'btn-danger',
    Ghost: 'btn-ghost',
  }[action.style];

  const loading = isLoading || action.isLoading;

  return (
    <button
      type="button"
      className={`modal-btn ${styleClass}`}
      onClick={onClick}
      disabled={action.isDisabled || loading}
    >
      {loading ? 'Loading...' : action.label}
    </button>
  );
}

// ============================================================================
// Alert Icon
// ============================================================================

function AlertIcon({ severity }: { severity: string }) {
  const icons: Record<string, string> = {
    Info: 'ℹ',
    Success: '✓',
    Warning: '⚠',
    Error: '✕',
  };

  return <span className="alert-icon">{icons[severity] ?? 'ℹ'}</span>;
}

// ============================================================================
// Specialized Modals - Delete Confirmation
// ============================================================================

export function DeleteConfirmation({
  itemType,
  itemName,
  itemId,
  deleteEndpoint,
  cancelUrl,
}: DeleteConfirmationProps) {
  const deleteMutation = useMutation<DeleteRequest, DeleteResponse>(deleteEndpoint, {
    onSuccess: (response) => {
      if (response.redirectUrl) {
        window.location.href = response.redirectUrl;
      }
    },
  });

  const handleDelete = () => {
    deleteMutation.mutate({ itemType, itemId });
  };

  const handleCancel = () => {
    navigate(cancelUrl);
  };

  const isLoading = deleteMutation.state.status === 'loading';

  return (
    <div className="modal-overlay" role="dialog" aria-modal="true">
      <div className="modal modal-sm">
        <div className="modal-header">
          <h2>Delete {itemType}</h2>
        </div>

        <div className="modal-body">
          <div className="modal-alert alert-warning">
            <AlertIcon severity="Warning" />
            <p>
              Are you sure you want to delete <strong>{itemName}</strong>?
              This action cannot be undone.
            </p>
          </div>
        </div>

        <div className="modal-footer">
          <button
            type="button"
            className="modal-btn btn-secondary"
            onClick={handleCancel}
            disabled={isLoading}
          >
            Cancel
          </button>
          <button
            type="button"
            className="modal-btn btn-danger"
            onClick={handleDelete}
            disabled={isLoading}
          >
            {isLoading ? 'Deleting...' : 'Delete'}
          </button>
        </div>
      </div>
    </div>
  );
}

// ============================================================================
// Specialized Modals - Edit Resident
// ============================================================================

export function EditResidentModal({
  residentId,
  currentName,
  currentRoom,
  currentAdmitDate,
  availableRooms,
}: EditResidentModalProps) {
  const [name, setName] = React.useState(currentName);
  const [room, setRoom] = React.useState(currentRoom);
  const [admitDate, setAdmitDate] = React.useState(
    currentAdmitDate ? new Date(currentAdmitDate).toISOString().split('T')[0] : ''
  );
  const [errors, setErrors] = React.useState<Record<string, string[]>>({});

  const updateMutation = useMutation<UpdateResidentRequest, UpdateResidentResponse>(
    '/residents/update',
    {
      onSuccess: (response) => {
        if (response.success) {
          window.location.reload();
        }
      },
      onError: (error) => {
        if ('errors' in error && error.errors) {
          setErrors(error.errors as Record<string, string[]>);
        }
      },
    }
  );

  const handleSubmit = (e: React.FormEvent) => {
    e.preventDefault();
    updateMutation.mutate({
      residentId,
      name,
      room,
      admitDate: admitDate,
    });
  };

  const handleCancel = () => {
    history.back();
  };

  const isLoading = updateMutation.state.status === 'loading';

  return (
    <div className="modal-overlay" role="dialog" aria-modal="true">
      <div className="modal modal-md">
        <div className="modal-header">
          <h2>Edit Resident</h2>
          <button type="button" className="modal-close" onClick={handleCancel} aria-label="Close">
            ×
          </button>
        </div>

        <form className="modal-body" onSubmit={handleSubmit}>
          <div className={`form-field ${errors.name?.length ? 'has-error' : ''}`}>
            <label htmlFor="name">
              Name <span className="required">*</span>
            </label>
            <input
              id="name"
              type="text"
              value={name}
              onChange={(e) => setName(e.target.value)}
              required
            />
            {errors.name?.map((err, i) => (
              <span key={i} className="error-message">{err}</span>
            ))}
          </div>

          <div className={`form-field ${errors.room?.length ? 'has-error' : ''}`}>
            <label htmlFor="room">
              Room <span className="required">*</span>
            </label>
            <select
              id="room"
              value={room}
              onChange={(e) => setRoom(e.target.value)}
              required
            >
              {availableRooms.map((r) => (
                <option key={r.roomNumber} value={r.roomNumber} disabled={!r.isAvailable}>
                  {r.roomNumber} - {r.buildingName} {!r.isAvailable && '(Occupied)'}
                </option>
              ))}
            </select>
            {errors.room?.map((err, i) => (
              <span key={i} className="error-message">{err}</span>
            ))}
          </div>

          <div className={`form-field ${errors.admitDate?.length ? 'has-error' : ''}`}>
            <label htmlFor="admitDate">
              Admit Date <span className="required">*</span>
            </label>
            <input
              id="admitDate"
              type="date"
              value={admitDate}
              onChange={(e) => setAdmitDate(e.target.value)}
              required
            />
            {errors.admitDate?.map((err, i) => (
              <span key={i} className="error-message">{err}</span>
            ))}
          </div>

          <div className="modal-footer">
            <button
              type="button"
              className="modal-btn btn-secondary"
              onClick={handleCancel}
              disabled={isLoading}
            >
              Cancel
            </button>
            <button type="submit" className="modal-btn btn-primary" disabled={isLoading}>
              {isLoading ? 'Saving...' : 'Save Changes'}
            </button>
          </div>
        </form>
      </div>
    </div>
  );
}
