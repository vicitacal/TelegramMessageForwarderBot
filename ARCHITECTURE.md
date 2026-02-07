# Architecture Overview

This project is a Telegram message forwarding bot.

## Architectural Style
- Clean Architecture
- Dependency direction: Presentation -> Application -> Domain

## Responsibilities

### Domain
- Pure business rules
- Message filtering logic
- No external dependencies

### Application
- Use cases
- Orchestration
- No Telegram API usage

### Infrastructure
- Telegram API integration
- Storage
- Secrets management

### Bot
- Command parsing
- Delegation to use cases
- No business logic

## Non-negotiable Rules
- No hardcoded secrets
- No static helpers
- No code-behind logic
- Always extensible