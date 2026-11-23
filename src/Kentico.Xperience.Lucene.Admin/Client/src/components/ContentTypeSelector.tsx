import {
  Colors,
  getDataTestId,
  Inline,
  DropDownOnClick,
  DropDownPlacement,
  Input,
  MenuItem,
  SelectMenu,
  Spacing,
  Stack,
  Tag,
  TextWithLabel,
} from "@kentico/xperience-admin-components";
import React, { useEffect, useRef, useState } from "react";

export interface ContentTypeSelectorOption {
  readonly value: string;
  readonly label: string;
}

export interface ContentTypeSelectorProps {
  readonly label: string;
  readonly options: readonly ContentTypeSelectorOption[];
  readonly selected: readonly string[];
  readonly onChange: (selected: string[]) => void;
  readonly disabled?: boolean;
  readonly dataTestId?: string;
  readonly explanationText?: string;
  /** Labels for selected values that are no longer options. */
  readonly selectedLabels?: Readonly<Record<string, string>>;
  /** Shows the "N selected · M available" summary. Defaults to true. */
  readonly showCount?: boolean;
}

const MAX_OPTIONS_HEIGHT = "16rem";

const OPTION_SELECTOR = '[data-testid="content-type-option"] > *';

/**
 * Searchable multi-select. The option list is a native anchored drop-down that opens
 * when the search box is focused or typed in, overlays the page without moving other
 * controls, and closes on Escape, an outside click or when focus leaves the selector.
 */
export const ContentTypeSelector = ({
  label,
  options,
  selected,
  onChange,
  disabled = false,
  dataTestId = "content-type-selector",
  explanationText,
  selectedLabels,
  showCount = true,
}: ContentTypeSelectorProps): React.JSX.Element => {
  const [search, setSearch] = useState("");
  const [open, setOpen] = useState(false);
  const [menuWidth, setMenuWidth] = useState<number>();
  const root = useRef<HTMLDivElement>(null);
  const anchor = useRef<HTMLElement | null>(null);
  const menu = useRef<HTMLDivElement>(null);
  const inputRef = useRef<HTMLInputElement>(null);

  const labelOf = (value: string): string => {
    const option = options.find((o) => o.value === value);
    return option
      ? option.label || option.value
      : `${selectedLabels?.[value] ?? value} (unavailable)`;
  };

  const toggle = (value: string): void => {
    onChange(
      selected.includes(value)
        ? selected.filter((v) => v !== value)
        : [...selected, value],
    );
  };

  const focusOption = (offset: number): void => {
    const items = Array.from(
      menu.current?.querySelectorAll<HTMLElement>(OPTION_SELECTOR) ?? [],
    );
    if (items.length === 0) {
      return;
    }
    const current = items.findIndex((i) => i === document.activeElement);
    const next =
      current === -1
        ? offset > 0
          ? 0
          : items.length - 1
        : (current + offset + items.length) % items.length;
    items[next]?.focus();
  };

  const openList = (): void => {
    setMenuWidth(anchor.current?.offsetWidth);
    setOpen(true);
  };

  const close = (): void => {
    setOpen(false);
    inputRef.current?.focus();
  };

  const term = search.trim().toLowerCase();
  const filtered =
    term === ""
      ? options
      : options.filter(
          (o) =>
            o.label.toLowerCase().includes(term) ||
            o.value.toLowerCase().includes(term),
        );
  const canOpen = !disabled && options.length > 0;
  const showList = open && canOpen;

  // Clicking a menu item may not move focus, so also close on pointer down outside.
  useEffect(() => {
    if (!showList) {
      return;
    }
    const onPointerDown = (e: PointerEvent): void => {
      const target = e.target as Node | null;
      if (!root.current?.contains(target) && !menu.current?.contains(target)) {
        setOpen(false);
      }
    };
    document.addEventListener("pointerdown", onPointerDown, true);
    return () => {
      document.removeEventListener("pointerdown", onPointerDown, true);
    };
  }, [showList]);

  return (
    <div
      role="group"
      aria-label={label}
      data-testid={dataTestId}
      ref={root}
      style={{ color: Colors.TextDefaultOnLight }}
      onBlur={(e) => {
        if (
          !root.current?.contains(e.relatedTarget) &&
          !menu.current?.contains(e.relatedTarget)
        ) {
          setOpen(false);
        }
      }}
      onKeyDown={(e) => {
        if (e.key === "Escape" && showList) {
          e.preventDefault();
          e.stopPropagation();
          close();
        }
      }}
    >
      <DropDownOnClick
        open={showList}
        placement={DropDownPlacement.BottomStart}
        onToggle={(isOpen) => {
          setOpen(isOpen && canOpen);
        }}
        renderTrigger={(triggerRef) => (
          // Anchor the menu to the whole selector so it opens below (or above) it and never covers the selected tags.
          <div
            ref={(element) => {
              (
                triggerRef as React.MutableRefObject<HTMLElement | null>
              ).current = element;
              anchor.current = element;
            }}
          >
            <Stack spacing={Spacing.S}>
              {showCount ? (
                <TextWithLabel
                  label={label}
                  value={`${String(selected.length)} selected · ${String(options.length)} available`}
                />
              ) : (
                <span
                  style={{ color: Colors.TextDefaultOnLight, fontSize: 14 }}
                >
                  {label}
                </span>
              )}
              {selected.length > 0 && (
                <div role="list" aria-label={`Selected ${label}`}>
                  <Inline spacing={Spacing.S}>
                    {selected.map((value) => (
                      <div role="listitem" key={value}>
                        <Tag
                          label={labelOf(value)}
                          removable={!disabled}
                          disabled={disabled}
                          onRemoveClick={() => {
                            toggle(value);
                          }}
                          dataTestId={getDataTestId("selected-content-type")}
                        />
                      </div>
                    ))}
                  </Inline>
                </div>
              )}
              <div
                onClick={openList}
                onKeyDown={(e) => {
                  if (e.key === "ArrowDown" || e.key === "ArrowUp") {
                    e.preventDefault();
                    const offset = e.key === "ArrowDown" ? 1 : -1;
                    openList();
                    requestAnimationFrame(() => {
                      focusOption(offset);
                    });
                  }
                }}
              >
                <Input
                  label={`Search ${label}`}
                  value={search}
                  inputRef={inputRef}
                  disabled={!canOpen}
                  placeholder={
                    options.length === 0
                      ? "No content types exist"
                      : "Type to filter, or press Down to browse"
                  }
                  explanationText={explanationText}
                  onChange={(e) => {
                    setSearch(e.target.value);
                    openList();
                  }}
                />
              </div>
            </Stack>
          </div>
        )}
      >
        <div
          ref={menu}
          role="group"
          aria-label={`${label} options`}
          data-testid="content-type-menu"
          style={{ width: menuWidth }}
          onKeyDown={(e) => {
            if (e.key === "Escape") {
              e.preventDefault();
              e.stopPropagation();
              close();
            } else if (e.key === "ArrowDown" || e.key === "ArrowUp") {
              e.preventDefault();
              focusOption(e.key === "ArrowDown" ? 1 : -1);
            }
          }}
        >
          <SelectMenu maxContentHeight={MAX_OPTIONS_HEIGHT}>
            {filtered.length === 0 ? (
              <MenuItem
                primaryLabel={`No content types match '${search}'.`}
                disabled
              />
            ) : (
              filtered.map((option) => {
                const isSelected = selected.includes(option.value);
                return (
                  <div
                    key={option.value}
                    data-testid="content-type-option"
                    data-value={option.value}
                    data-selected={isSelected}
                    onKeyDown={(e) => {
                      if (e.key === " ") {
                        e.preventDefault();
                        toggle(option.value);
                      }
                    }}
                  >
                    <MenuItem
                      primaryLabel={option.label || option.value}
                      secondaryLabel={
                        isSelected ? `Selected · ${option.value}` : option.value
                      }
                      isMultiSelect
                      selected={isSelected}
                      onClick={() => {
                        toggle(option.value);
                      }}
                    />
                  </div>
                );
              })
            )}
          </SelectMenu>
        </div>
      </DropDownOnClick>
    </div>
  );
};
