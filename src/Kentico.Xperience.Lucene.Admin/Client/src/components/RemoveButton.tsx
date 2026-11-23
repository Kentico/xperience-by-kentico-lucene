import {
  Button,
  ButtonColor,
  ButtonType,
} from "@kentico/xperience-admin-components";
import * as React from "react";

export interface RemoveButtonProps {
  /** Hover tooltip and accessible name, for example "Remove channel Dancing Goat". */
  readonly title: string;
  readonly onClick: () => void;
  readonly disabled?: boolean;
  readonly inProgress?: boolean;
}

/** Native icon-only destructive trash button used for remove and delete actions. */
export const RemoveButton = ({
  title,
  onClick,
  disabled = false,
  inProgress = false,
}: RemoveButtonProps): React.JSX.Element => {
  const buttonRef = React.useRef<HTMLButtonElement>(null);
  // The native icon-only Button names itself "button"; use the title as its accessible name.
  React.useLayoutEffect(() => {
    buttonRef.current?.setAttribute("aria-label", title);
  }, [title]);
  return (
    <Button
      type={ButtonType.Button}
      buttonRef={buttonRef}
      icon="xp-bin"
      title={title}
      color={ButtonColor.Quinary}
      destructive
      disabled={disabled}
      inProgress={inProgress}
      onClick={onClick}
    />
  );
};
