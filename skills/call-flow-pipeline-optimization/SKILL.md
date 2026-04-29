# Skill: call-flow-pipeline-optimization

## Purpose
Optimize live voice conversation pipeline for speed, natural interaction quality, reliability, and cost efficiency across web and phone channels.

## Goal
Make every live voice conversation fast, natural, cheap, and reliable.

## Core rule
The agent must listen first, understand intent quickly, collect only missing information, use tools for facts/prices/bookings, speak short responses, and never send unnecessary data to LLM, STT, or TTS providers.

## End-to-end pipeline
1. Receive audio/text input.
2. Detect speech end or user interruption.
3. Convert speech to text using streaming STT.
4. Normalize transcript.
5. Detect intent.
6. Load tenant/client/campaign config from cache.
7. Load active conversation state from memory.
8. Extract slots from latest user message.
9. Merge extracted slots with existing state.
10. Identify missing required slots.
11. If slots are missing, ask only one clear question.
12. If all slots exist, validate them.
13. Execute required tool/service.
14. Generate short human-like reply.
15. Send final reply to TTS.
16. Stream audio back to user.
17. Save transcript, state, tool logs, and cost logs.
18. Continue or close conversation.

## Optimization rules
- Do not send full menu, full campaign config, or full conversation history to the LLM.
- Send only the last 3–5 turns plus current state summary.
- Use deterministic services for price, distance, delivery fee, tax, totals, availability, and booking confirmation.
- Use RAG only for FAQs, policies, scripts, and service explanations.
- Never use RAG for official prices or confirmed booking data.
- Cache tenant config, campaign config, menu categories, common FAQs, prompt templates, and TTS common phrases.
- Use short replies unless the user asks for detail.
- Ask one question at a time.
- Do not repeat full order after every item.
- Only confirm full order at final confirmation stage.
- Use tool results as source of truth.
- If user interrupts, stop speaking and listen immediately.
- If user is silent, ask once simply, then close or fallback based on campaign rule.
- If confidence is low, ask clarification instead of guessing.
- If external API fails, save result internally and mark `CapturedPendingSync`.
- If human transfer is disabled, never promise human transfer.
- If human transfer is enabled and required, transfer through FreeSWITCH.

## LLM token optimization
- Use small extraction prompts for intent and slot extraction.
- Use campaign-specific compact prompts.
- Do not include unrelated campaign rules.
- Include only relevant menu/deal/search results.
- Summarize old conversation history.
- Use JSON output for extraction.
- Avoid long system prompts in every turn by caching prompt templates.

## ElevenLabs optimization
- Send only the final user-facing bot response.
- Keep responses short and natural.
- Cache common phrases:
  - "Hi, how can I help you today?"
  - "Would you like anything else?"
  - "Can you please repeat that?"
  - "Your order has been saved."
- Do not generate TTS for internal reasoning, tool results, or hidden system messages.
- Avoid repeating long order summaries until final confirmation.

## Deepgram optimization
- Use streaming STT.
- Use endpointing/silence detection.
- Stop listening while bot is speaking unless barge-in is enabled.
- Close stream immediately when call ends.
- Avoid processing long silence.
- Track STT audio seconds per call.

## Human-like conversation rules
- Speak like a helpful operator, not a chatbot.
- Keep replies under 1–2 sentences during active slot collection.
- Acknowledge user input briefly.
- Ask the next needed question naturally.
- Use contextual acknowledgements (example: "Got it, two chicken burgers.").
- Do not over-explain.
- Avoid robotic language.

## Restaurant flow optimization
- If user asks for menu, show categories first.
- If user asks for deals, show available deals.
- If user asks for a dish, search matching items.
- If user adds item, validate menu item first.
- If deal has choices, ask only missing choices.
- Calculate totals using pricing service.
- Confirm full order only before saving.

## Courier flow optimization
- Collect pickup, drop-off, weight, package type, urgency.
- Use OpenStreetMap/Nominatim for coordinates.
- Use OSRM for distance/duration.
- Apply tenant courier pricing rules.
- Do not quote price unless distance and weight are valid.
- If address is unclear, ask user to clarify.

## Call state optimization
- Store live state in memory.
- Persist every important turn to PostgreSQL.
- Keep current cart/quote in memory during active call.
- Save final result as JSONB.
- Use `CallSessionId` and `CorrelationId` everywhere.

## Failure handling
- LLM failure: retry once, then fallback.
- STT failure: ask user to repeat.
- TTS failure: return text or fallback audio if web demo.
- Tool failure: log and use safe fallback.
- External API failure: save internally and retry via worker.
- User confusion: simplify question.
- Repeated failure: close, save, or handoff based on campaign config.

## Performance targets and telemetry
- Bot should begin responding within 1–1.5 seconds after user stops speaking.
- Every provider call must log latency.
- Every call must track:
  - LLM input tokens
  - LLM output tokens
  - TTS characters
  - STT seconds
  - Call duration
  - Estimated cost

## Completion checklist
- Pipeline enforces one-question-at-a-time slot collection.
- LLM context is bounded (3–5 turns + state summary).
- Deterministic services own pricing/booking truth.
- Interruption, silence, and fallback behavior is explicitly implemented.
- Cost and latency telemetry is emitted for all provider/tool calls.
