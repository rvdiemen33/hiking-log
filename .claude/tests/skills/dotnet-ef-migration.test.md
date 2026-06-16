# Evals: dotnet-ef-migration skill

Each test contains a prompt, the expected behavior, and pass criteria.
Send the prompt in a fresh session and verify that the skill is loaded.

---

## Positive tests — skill must activate

### Test 1: New migration after entity change

**Prompt:**
> I added a property to the HikeLog entity. How do I create a migration?

**Expected behavior:**
Skill loads. Claude provides the correct `dotnet ef migrations add` command with both project flags.

**Pass criteria:**
- [ ] Command contains `--project src/HikingLog.Infrastructure`
- [ ] Command contains `--startup-project src/HikingLog.Api`
- [ ] Claude mentions the naming convention (PascalCase, descriptive)

---

### Test 2: Update the database

**Prompt:**
> Update the database to the latest migration.

**Expected behavior:**
Skill loads. Claude provides the `dotnet ef database update` command with both project flags.

**Pass criteria:**
- [ ] Correct command with both flags
- [ ] No mention of `--project` without `--startup-project`

---

### Test 3: General EF command question

**Prompt:**
> Which EF commands do I need to run after modifying the DbContext?

**Expected behavior:**
Skill loads. Claude describes the full workflow: build → migrations add → review → database update → test.

**Pass criteria:**
- [ ] Workflow is complete (steps 1 through 6)
- [ ] Both project flags present in every command

---

### Test 4: Undo a migration

**Prompt:**
> I created a wrong migration that has not been applied yet. How do I remove it?

**Expected behavior:**
Skill loads. Claude provides `dotnet ef migrations remove` with the correct flags and warns that this only works if the migration has not been applied.

**Pass criteria:**
- [ ] `migrations remove` command present
- [ ] Warning that the migration must not have been applied

---

### Test 5: Schema change after new entity (implicit)

**Prompt:**
> I extended the Route entity with a GPX field. What are the next steps?

**Expected behavior:**
Skill loads. Claude describes build → create migration → apply as part of the next steps.

**Pass criteria:**
- [ ] Migration step explicitly mentioned
- [ ] Correct commands with project flags

---

## Negative tests — skill must NOT activate

### Test N1: Create a new controller

**Prompt:**
> Create a RoutesController with CRUD endpoints.

**Expected behavior:**
Skill does NOT load. Claude creates the controller without migration context.

---

### Test N2: General architecture question

**Prompt:**
> Explain the Clean Architecture layer separation for this project.

**Expected behavior:**
Skill does NOT load. Claude answers based on CLAUDE.md without migration context.

---

### Test N3: Create entity without database action

**Prompt:**
> Create the HikeLog class in the domain project.

**Expected behavior:**
Skill does NOT load (no migration needed yet — the entity does not exist in the DbContext).

---

## Verifying activation

Use `/context` in Claude Code after sending the prompt to see which skills are loaded.
If `dotnet-ef-migration` is visible under the loaded skills, the test passed.
