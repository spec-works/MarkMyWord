# Admin Governance Over Tool Calls: Permissions, Not Principals

**Source:** "Manifest Schema for MCP Certification" meeting — March 17, 2026

**Speaker:** Darrel Miller

---

## Context

During the later portion of the meeting, Sachin Joshi proposed providing an admin "control surface" to enable and disable individual tool usage. Darrel Miller pushed back strongly, arguing that tool governance must be handled through the existing OAuth2 permissions model rather than by inventing new per-tool control mechanisms or treating "tool" as a new security principal.

## Core Position: Use Permissions to Control Tool Access

> "OK, I'm fighting really, really hard to help to stop that from happening because it's not realistic to control and I don't want us to do that. What I'm telling people is we need to use permissions as a way of controlling access to tools."

> "And if you want to, if the maker wants to be able to enable and disable different tools, then they should create permissions for the different tools and turn on and off permissions 'cause that's our security model."

## Justification #1: Don't Invent Runtime Enforcement — Let OAuth2 Do It

When asked who would be responsible for enforcement of tool permissions, Darrel was emphatic that the existing OAuth2 infrastructure should handle it:

> "Well, see, I don't want you to have to be responsible for enforcement. I want OAuth2 to do it."

He acknowledged that static vs. dynamic tool selection is a valid concern for prompt injection, but drew a clear line at execution time:

> "So there is the question of dynamic tool versus static tool and at the moment the admin folks are very, very concerned about enabling dynamic tools because they don't believe admins will be comfortable accepting it. So it's perfectly fine for us to say yes, we're only going to take these tools that are here and put them in the prompt, the ones that have been approved by the admin, and only inject those into the prompt. So that the LLM will only call them. The MCP server might have ten other tools, but the LLM is never going to see them, so that's fine."

> "But when it actually comes to executing the tool and the tool going over the wire, it is our responsibility to make sure that there's the right permissions that have been consented and go in the token, but it is the customer's responsibility to do authz on that endpoint. I do not want us inventing new runtime mechanisms that are going to guarantee that that tool will never be called because we're just inventing new security mechanisms."

## Justification #2: Per-Tool Toggles Break MCP Servers Due to Interdependencies

Darrel explained that allowing admins to toggle individual tools is dangerous because tools can depend on each other:

> "The MCP owner is responsible for providing the level of granularity of enabling and disabling the tools that they think is appropriate."

> "The reason why I pushed back yesterday against Anurag is we can't enable turning on and turning off because there may be interdependencies between those tools, and if an admin turns off one and not the other, they could completely break the functionality of the MCP server."

He then provided a concrete example of how the ISV should model this via permissions:

> "So if, for example, you have an MCP server that has a bunch of read functionality and a bunch of write functionality, and the maker, the ISV, learns that admins are OK with the read, but they don't want to turn on the write, then they should go and mint 2 permissions for their MCP server. One that's read and one that's write, and just like we do in all of our other plugins in the store, we show the admin the permissions that this thing requires and they get to choose which permissions to consent to the client ID in order to be able to make the call, but not at a tool call level."

## Justification #3: Don't Treat "Tool" as a New Security Principal

This is the central argument — reuse existing, audited permission infrastructure rather than creating a new security primitive:

> "Right, because we already have all that infrastructure in place, it's already tested. We already have all the admin controls on it. We get visibility into those permissions. You can do auditing of those. We don't have to rebuild all of that infrastructure."

> "Imagine if we went and we built a new security principal which was tool. We'd have to rebuild all of that stuff again."

## Extension: This Works Beyond Entra ID

When Sachin asked about non-Microsoft IDPs (e.g., Okta), Darrel pointed out that permissions and scopes are universal:

> "They have permissions too."

> "It's on protected resource metadata. That's where you advertise what the scopes are that are required for the MCP server."

> "Well, I mean, you have to provide it on your MCP server as part of the MCP spec, but all the IDP providers support the notion of permissions, claims, scopes, whatever you want to call them, right?"

---

## Summary

Darrel's position can be distilled to three principles:

1. **Permissions, not toggles** — Tool access should be governed by OAuth2 scopes/permissions that admins consent to, not by per-tool enable/disable switches that the platform must invent and enforce at runtime.

2. **ISV-defined granularity** — The MCP server owner (ISV) decides the right level of permission granularity for their tools, because only they understand tool interdependencies.

3. **No new security principals** — "Tool" should not become a new security principal. The existing permission infrastructure (admin consent, auditing, visibility) already handles this. Rebuilding it for a "tool" principal would duplicate tested infrastructure unnecessarily.
