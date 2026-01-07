import * as React from 'react';
import { useMutation, navigate } from '../Shared/runtime';
import type {
  PaneContainerProps,
  PaneConfig,
  PaneContent,
  PaneAction,
  PaneSection,
  PaneSectionItem,
  PaneActionRequest,
  PaneActionResponse,
  ResidentDetailPaneProps,
  ActivityFeedPaneProps,
  ActivityItemData,
  FilterPaneProps,
  FilterGroupData,
  ApplyFiltersRequest,
  ApplyFiltersResponse,
} from '../Shared/types.g';

// ============================================================================
// Pane Container - Manages pane state and rendering
// ============================================================================

export function PaneContainer({ isOpen, pane, returnUrl }: PaneContainerProps) {
  const actionMutation = useMutation<PaneActionRequest, PaneActionResponse>('/pane/action', {
    onSuccess: (response) => {
      if (response.redirectUrl) {
        window.location.href = response.redirectUrl;
      } else if (response.updatedPane) {
        window.location.reload();
      }
    },
  });

  const handleClose = React.useCallback(() => {
    if (returnUrl) {
      navigate(returnUrl);
    }
  }, [returnUrl]);

  const handleAction = React.useCallback((action: PaneAction) => {
    switch (action.type) {
      case 'Close':
        handleClose();
        break;
      case 'Navigate':
        if (action.endpoint) {
          navigate(action.endpoint);
        }
        break;
      case 'Submit':
        if (action.endpoint) {
          actionMutation.mutate({
            paneId: pane?.id ?? '',
            actionId: action.id,
            data: undefined,
          });
        }
        break;
      case 'OpenModal':
        if (action.endpoint) {
          navigate(action.endpoint);
        }
        break;
      case 'Custom':
        // Custom action handled by specific implementations
        break;
    }
  }, [handleClose, actionMutation, pane?.id]);

  if (!isOpen || !pane) {
    return null;
  }

  return (
    <Pane
      config={pane}
      onClose={handleClose}
      onAction={handleAction}
      isLoading={actionMutation.state.status === 'loading'}
    />
  );
}

// ============================================================================
// Base Pane Component
// ============================================================================

interface PaneProps {
  config: PaneConfig;
  onClose: () => void;
  onAction: (action: PaneAction) => void;
  isLoading: boolean;
}

function Pane({ config, onClose, onAction, isLoading }: PaneProps) {
  const paneRef = React.useRef<HTMLDivElement>(null);

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

  // Focus management
  React.useEffect(() => {
    const pane = paneRef.current;
    if (!pane) return;

    const focusable = pane.querySelector<HTMLElement>(
      'button, [href], input, select, textarea, [tabindex]:not([tabindex="-1"])'
    );
    focusable?.focus();
  }, []);

  const handleOverlayClick = (e: React.MouseEvent) => {
    if (config.closeOnOverlay && e.target === e.currentTarget) {
      onClose();
    }
  };

  const positionClass = {
    Right: 'pane-right',
    Left: 'pane-left',
    Bottom: 'pane-bottom',
    Top: 'pane-top',
  }[config.position];

  const sizeClass = {
    Small: 'pane-sm',
    Medium: 'pane-md',
    Large: 'pane-lg',
    Half: 'pane-half',
    Full: 'pane-full',
  }[config.size];

  return (
    <div className="pane-overlay" onClick={handleOverlayClick}>
      <div
        ref={paneRef}
        className={`pane ${positionClass} ${sizeClass}`}
        role="dialog"
        aria-modal="true"
        aria-labelledby="pane-title"
      >
        <header className="pane-header">
          <div className="pane-header-content">
            <h2 id="pane-title">{config.title}</h2>
            {config.subtitle && <p className="pane-subtitle">{config.subtitle}</p>}
          </div>

          <div className="pane-header-actions">
            {config.headerActions?.map((action) => (
              <PaneActionButton
                key={action.id}
                action={action}
                onClick={() => onAction(action)}
                isLoading={isLoading && action.type === 'Submit'}
              />
            ))}
            {config.showCloseButton && (
              <button
                type="button"
                className="pane-close"
                onClick={onClose}
                aria-label="Close"
              >
                ×
              </button>
            )}
          </div>
        </header>

        <div className="pane-body">
          <PaneContentRenderer content={config.content} />
        </div>

        {config.footerActions && config.footerActions.length > 0 && (
          <footer className="pane-footer">
            {config.footerActions.map((action) => (
              <PaneActionButton
                key={action.id}
                action={action}
                onClick={() => onAction(action)}
                isLoading={isLoading && action.type === 'Submit'}
              />
            ))}
          </footer>
        )}
      </div>
    </div>
  );
}

// ============================================================================
// Pane Content Renderer
// ============================================================================

function PaneContentRenderer({ content }: { content: PaneContent }) {
  switch (content.type) {
    case 'Sections':
      return (
        <div className="pane-sections">
          {content.sections?.map((section) => (
            <PaneSectionRenderer key={section.id} section={section} />
          ))}
        </div>
      );

    case 'Component':
      // Component would be loaded from registry
      return (
        <div
          className="pane-component"
          data-component={content.componentPath}
          data-props={JSON.stringify(content.componentProps)}
        />
      );

    case 'Iframe':
      return (
        <iframe
          src={content.url ?? ''}
          className="pane-iframe"
          title="Pane content"
          frameBorder="0"
        />
      );

    case 'Loading':
      return (
        <div className="pane-loading">
          <div className="spinner" />
          <p>Loading...</p>
        </div>
      );

    default:
      return null;
  }
}

// ============================================================================
// Pane Section Renderer
// ============================================================================

function PaneSectionRenderer({ section }: { section: PaneSection }) {
  const [isCollapsed, setIsCollapsed] = React.useState(section.isCollapsed);

  const toggleCollapse = () => {
    if (section.isCollapsible) {
      setIsCollapsed(!isCollapsed);
    }
  };

  return (
    <section className={`pane-section ${isCollapsed ? 'collapsed' : ''}`}>
      <h3
        className={`pane-section-title ${section.isCollapsible ? 'collapsible' : ''}`}
        onClick={toggleCollapse}
        role={section.isCollapsible ? 'button' : undefined}
        aria-expanded={section.isCollapsible ? !isCollapsed : undefined}
      >
        {section.title}
        {section.isCollapsible && (
          <span className="collapse-icon">{isCollapsed ? '▶' : '▼'}</span>
        )}
      </h3>

      {!isCollapsed && (
        <dl className="pane-section-items">
          {section.items.map((item, index) => (
            <PaneSectionItemRenderer key={index} item={item} />
          ))}
        </dl>
      )}
    </section>
  );
}

function PaneSectionItemRenderer({ item }: { item: PaneSectionItem }) {
  const renderValue = () => {
    if (!item.value) return <span className="empty">—</span>;

    switch (item.type) {
      case 'Link':
        return (
          <a href={item.href ?? '#'} className="pane-link">
            {item.value}
          </a>
        );

      case 'Badge':
        return <span className="pane-badge">{item.value}</span>;

      case 'Date':
        return (
          <time dateTime={item.value}>
            {new Date(item.value).toLocaleDateString()}
          </time>
        );

      case 'Currency':
        return (
          <span className="pane-currency">
            ${parseFloat(item.value).toLocaleString('en-US', { minimumFractionDigits: 2 })}
          </span>
        );

      case 'Status':
        return (
          <span className={`pane-status status-${item.value.toLowerCase()}`}>
            {item.value}
          </span>
        );

      default:
        return <span>{item.value}</span>;
    }
  };

  return (
    <>
      <dt>
        {item.icon && <span className="item-icon">{item.icon}</span>}
        {item.label}
      </dt>
      <dd>{renderValue()}</dd>
    </>
  );
}

// ============================================================================
// Pane Action Button
// ============================================================================

interface ActionButtonProps {
  action: PaneAction;
  onClick: () => void;
  isLoading: boolean;
}

function PaneActionButton({ action, onClick, isLoading }: ActionButtonProps) {
  const styleClass = {
    Primary: 'btn-primary',
    Secondary: 'btn-secondary',
    Danger: 'btn-danger',
    Ghost: 'btn-ghost',
    Icon: 'btn-icon',
  }[action.style];

  const loading = isLoading || action.isLoading;

  return (
    <button
      type="button"
      className={`pane-btn ${styleClass}`}
      onClick={onClick}
      disabled={action.isDisabled || loading}
      aria-label={action.style === 'Icon' ? action.label : undefined}
    >
      {action.icon && <span className="btn-icon">{action.icon}</span>}
      {action.style !== 'Icon' && (loading ? 'Loading...' : action.label)}
    </button>
  );
}

// ============================================================================
// Specialized Panes - Resident Detail
// ============================================================================

export function ResidentDetailPane({
  residentId,
  name,
  photoUrl,
  status,
  infoSections,
  actions,
}: ResidentDetailPaneProps) {
  const handleAction = (action: PaneAction) => {
    if (action.endpoint) {
      navigate(action.endpoint);
    }
  };

  const statusClass = {
    Active: 'status-active',
    Discharged: 'status-discharged',
    OnLeave: 'status-onleave',
    Pending: 'status-pending',
  }[status];

  return (
    <div className="resident-detail-pane">
      <div className="resident-header">
        <img src={photoUrl} alt={name} className="resident-photo" />
        <div className="resident-info">
          <h2>{name}</h2>
          <span className={`resident-status ${statusClass}`}>{status}</span>
        </div>
      </div>

      <div className="resident-actions">
        {actions.map((action) => (
          <PaneActionButton
            key={action.id}
            action={action}
            onClick={() => handleAction(action)}
            isLoading={false}
          />
        ))}
      </div>

      <div className="resident-sections">
        {infoSections.map((section) => (
          <PaneSectionRenderer key={section.id} section={section} />
        ))}
      </div>
    </div>
  );
}

// ============================================================================
// Specialized Panes - Activity Feed
// ============================================================================

export function ActivityFeedPane({
  title,
  activities,
  hasMore,
  loadMoreUrl,
}: ActivityFeedPaneProps) {
  return (
    <div className="activity-feed-pane">
      <h2 className="feed-title">{title}</h2>

      <ul className="activity-list">
        {activities.map((activity) => (
          <ActivityItem key={activity.id} activity={activity} />
        ))}
      </ul>

      {hasMore && (
        <button
          type="button"
          className="load-more-btn"
          onClick={() => navigate(loadMoreUrl)}
        >
          Load More
        </button>
      )}
    </div>
  );
}

function ActivityItem({ activity }: { activity: ActivityItemData }) {
  return (
    <li className={`activity-item activity-${activity.type.toLowerCase()}`}>
      {activity.actorAvatar && (
        <img src={activity.actorAvatar} alt="" className="activity-avatar" />
      )}
      <div className="activity-content">
        {activity.actorName && (
          <span className="activity-actor">{activity.actorName}</span>
        )}
        <span className="activity-description">{activity.description}</span>
        <time className="activity-time" dateTime={activity.timestamp as unknown as string}>
          {formatRelativeTime(activity.timestamp as unknown as string)}
        </time>
      </div>
    </li>
  );
}

function formatRelativeTime(timestamp: string): string {
  const date = new Date(timestamp);
  const now = new Date();
  const diffMs = now.getTime() - date.getTime();
  const diffMins = Math.floor(diffMs / 60000);
  const diffHours = Math.floor(diffMs / 3600000);
  const diffDays = Math.floor(diffMs / 86400000);

  if (diffMins < 1) return 'Just now';
  if (diffMins < 60) return `${diffMins}m ago`;
  if (diffHours < 24) return `${diffHours}h ago`;
  if (diffDays < 7) return `${diffDays}d ago`;
  return date.toLocaleDateString();
}

// ============================================================================
// Specialized Panes - Filter Pane
// ============================================================================

export function FilterPane({
  title,
  filterGroups,
  currentFilters,
  applyUrl,
  resetUrl,
}: FilterPaneProps) {
  const [filters, setFilters] = React.useState<Record<string, unknown>>(
    currentFilters as Record<string, unknown>
  );

  const applyMutation = useMutation<ApplyFiltersRequest, ApplyFiltersResponse>(applyUrl, {
    onSuccess: (response) => {
      if (response.redirectUrl) {
        window.location.href = response.redirectUrl;
      }
    },
  });

  const handleFilterChange = (groupId: string, value: unknown) => {
    setFilters((prev) => ({ ...prev, [groupId]: value }));
  };

  const handleApply = () => {
    applyMutation.mutate({ filters });
  };

  const handleReset = () => {
    setFilters({});
    navigate(resetUrl);
  };

  const isLoading = applyMutation.state.status === 'loading';

  return (
    <div className="filter-pane">
      <h2 className="filter-title">{title}</h2>

      <div className="filter-groups">
        {filterGroups.map((group) => (
          <FilterGroup
            key={group.id}
            group={group}
            value={filters[group.id]}
            onChange={(value) => handleFilterChange(group.id, value)}
          />
        ))}
      </div>

      <div className="filter-actions">
        <button
          type="button"
          className="btn-secondary"
          onClick={handleReset}
          disabled={isLoading}
        >
          Reset
        </button>
        <button
          type="button"
          className="btn-primary"
          onClick={handleApply}
          disabled={isLoading}
        >
          {isLoading ? 'Applying...' : 'Apply Filters'}
        </button>
      </div>
    </div>
  );
}

interface FilterGroupProps {
  group: FilterGroupData;
  value: unknown;
  onChange: (value: unknown) => void;
}

function FilterGroup({ group, value, onChange }: FilterGroupProps) {
  switch (group.type) {
    case 'Checkbox':
      return (
        <fieldset className="filter-group">
          <legend>{group.label}</legend>
          {group.options?.map((option) => {
            const values = (value as string[]) ?? [];
            return (
              <label key={option.value} className="filter-option">
                <input
                  type="checkbox"
                  checked={values.includes(option.value)}
                  onChange={(e) => {
                    if (e.target.checked) {
                      onChange([...values, option.value]);
                    } else {
                      onChange(values.filter((v) => v !== option.value));
                    }
                  }}
                />
                {option.label}
                {option.count !== null && option.count !== undefined && (
                  <span className="filter-count">({option.count})</span>
                )}
              </label>
            );
          })}
        </fieldset>
      );

    case 'Radio':
      return (
        <fieldset className="filter-group">
          <legend>{group.label}</legend>
          {group.options?.map((option) => (
            <label key={option.value} className="filter-option">
              <input
                type="radio"
                name={group.id}
                checked={value === option.value}
                onChange={() => onChange(option.value)}
              />
              {option.label}
            </label>
          ))}
        </fieldset>
      );

    case 'Range':
      return (
        <fieldset className="filter-group">
          <legend>{group.label}</legend>
          <div className="filter-range">
            <input
              type="number"
              placeholder="Min"
              min={group.min != null ? Number(group.min) : undefined}
              value={((value as { min?: number })?.min) ?? ''}
              onChange={(e) =>
                onChange({ ...(value as object), min: e.target.value ? parseFloat(e.target.value) : undefined })
              }
            />
            <span>to</span>
            <input
              type="number"
              placeholder="Max"
              max={group.max != null ? Number(group.max) : undefined}
              value={((value as { max?: number })?.max) ?? ''}
              onChange={(e) =>
                onChange({ ...(value as object), max: e.target.value ? parseFloat(e.target.value) : undefined })
              }
            />
          </div>
        </fieldset>
      );

    case 'DateRange':
      return (
        <fieldset className="filter-group">
          <legend>{group.label}</legend>
          <div className="filter-date-range">
            <input
              type="date"
              value={((value as { start?: string })?.start) ?? ''}
              onChange={(e) => onChange({ ...(value as object), start: e.target.value })}
            />
            <span>to</span>
            <input
              type="date"
              value={((value as { end?: string })?.end) ?? ''}
              onChange={(e) => onChange({ ...(value as object), end: e.target.value })}
            />
          </div>
        </fieldset>
      );

    case 'Search':
      return (
        <fieldset className="filter-group">
          <legend>{group.label}</legend>
          <input
            type="search"
            placeholder="Search..."
            value={(value as string) ?? ''}
            onChange={(e) => onChange(e.target.value)}
          />
        </fieldset>
      );

    default:
      return null;
  }
}
