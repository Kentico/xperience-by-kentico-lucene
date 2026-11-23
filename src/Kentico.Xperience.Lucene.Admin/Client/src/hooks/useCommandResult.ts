import { usePageCommand } from "@kentico/xperience-admin-base";
import * as React from "react";

// Registered page commands return void; the framework delivers the envelope's
// result to `after`, not to the command provider's execution promise.
// eslint-disable-next-line @typescript-eslint/no-unnecessary-type-parameters
export const useCommandResult = <TResult, TData = void>(
  name: string,
): ((data?: TData) => Promise<TResult>) => {
  const result = React.useRef<TResult>();
  const active = React.useRef<AbortController | null>(null);
  const { execute } = usePageCommand<TResult, TData>(name, {
    after: (value) => {
      result.current = value;
    },
  });
  const executeRef = React.useRef(execute);
  React.useLayoutEffect(() => {
    executeRef.current = execute;
  }, [execute]);
  const getResult = React.useCallback(() => result.current, []);

  React.useEffect(
    () => () => {
      active.current?.abort();
    },
    [],
  );

  return React.useCallback(
    async (data?: TData): Promise<TResult> => {
      if (active.current) {
        throw new Error("A command is already running. Please wait.");
      }

      const controller = new AbortController();
      active.current = controller;
      result.current = undefined;
      let timer: ReturnType<typeof setTimeout> | undefined;

      try {
        await Promise.race([
          executeRef.current(data, undefined, controller),
          new Promise<never>((_, reject) => {
            timer = setTimeout(() => {
              controller.abort();
              reject(new Error("The request timed out. Please try again."));
            }, 60_000);
          }),
        ]);

        const value = getResult();
        if (value === undefined || value === null) {
          throw new Error(
            "The server did not return a result. Please try again.",
          );
        }

        return value;
      } finally {
        clearTimeout(timer);
        controller.abort();
        active.current = null;
      }
    },
    [getResult],
  );
};
