import ky from "ky";
import { describe, expect, it } from "vitest";

import { createQueryClient } from "./query-client";

function failingWith(status: number) {
  let calls = 0;
  const client = ky.create({
    prefix: "http://api.test",
    retry: 0,
    fetch: async () => {
      calls++;
      return new Response(null, { status });
    },
  });
  return { request: () => client.get("thing").json(), calls: () => calls };
}

describe("query client", () => {
  it.each([400, 401, 403, 404, 429])("does not retry a %i response", async (status) => {
    const queryClient = createQueryClient();
    const api = failingWith(status);

    await expect(queryClient.fetchQuery({ queryKey: ["thing"], queryFn: api.request, retryDelay: 0 })).rejects.toThrow();
    expect(api.calls()).toBe(1);
  });

  it("retries server errors twice", async () => {
    const queryClient = createQueryClient();
    const api = failingWith(503);

    await expect(queryClient.fetchQuery({ queryKey: ["thing"], queryFn: api.request, retryDelay: 0 })).rejects.toThrow();
    expect(api.calls()).toBe(3);
  });
});
