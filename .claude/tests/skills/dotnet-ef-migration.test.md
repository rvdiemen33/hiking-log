# Evals: dotnet-ef-migration skill

Elke test bevat een prompt, het verwachte gedrag en de slaagcriteria.
Stuur de prompt in een verse sessie en beoordeel of de skill geladen wordt.

---

## Positieve tests — skill moet activeren

### Test 1: Nieuwe migratie na entiteitswijziging

**Prompt:**
> Ik heb een property toegevoegd aan de HikeLog-entiteit. Hoe maak ik een migratie aan?

**Verwacht gedrag:**
Skill laadt. Claude geeft het juiste `dotnet ef migrations add`-commando met beide projectvlaggen.

**Slaagcriteria:**
- [ ] Commando bevat `--project src/HikingLog.Infrastructure`
- [ ] Commando bevat `--startup-project src/HikingLog.Api`
- [ ] Claude noemt naamgevingsconventie (PascalCase, beschrijvend)

---

### Test 2: Database bijwerken

**Prompt:**
> Update de database naar de nieuwste migratie.

**Verwacht gedrag:**
Skill laadt. Claude geeft het `dotnet ef database update`-commando met beide projectvlaggen.

**Slaagcriteria:**
- [ ] Correct commando met beide vlaggen
- [ ] Geen vermelding van `--project` zonder `--startup-project`

---

### Test 3: EF-commando algemeen

**Prompt:**
> Welke EF-commando's moet ik uitvoeren na het aanpassen van de DbContext?

**Verwacht gedrag:**
Skill laadt. Claude beschrijft de volledige workflow: build → migrations add → review → database update → test.

**Slaagcriteria:**
- [ ] Workflow is volledig (stap 1 t/m 6)
- [ ] Beide projectvlaggen aanwezig in elk commando

---

### Test 4: Migratie ongedaan maken

**Prompt:**
> Ik heb een foute migratie aangemaakt die nog niet is toegepast. Hoe verwijder ik hem?

**Verwacht gedrag:**
Skill laadt. Claude geeft `dotnet ef migrations remove` met de juiste vlaggen en waarschuwt dat dit alleen werkt als de migratie nog niet is toegepast.

**Slaagcriteria:**
- [ ] `migrations remove` commando aanwezig
- [ ] Waarschuwing dat migratie niet toegepast mag zijn

---

### Test 5: Schema-verandering na nieuwe entiteit (impliciet)

**Prompt:**
> Ik heb de Route-entiteit uitgebreid met een GPX-veld. Wat zijn de volgende stappen?

**Verwacht gedrag:**
Skill laadt. Claude beschrijft build → migratie aanmaken → toepassen als onderdeel van de volgende stappen.

**Slaagcriteria:**
- [ ] Migratiestap wordt expliciet genoemd
- [ ] Correcte commando's met projectvlaggen

---

## Negatieve tests — skill mag NIET activeren

### Test N1: Nieuwe controller aanmaken

**Prompt:**
> Maak een RoutesController aan met CRUD-endpoints.

**Verwacht gedrag:**
Skill laadt NIET. Claude maakt de controller zonder migratiecontext.

---

### Test N2: Algemene architectuurvraag

**Prompt:**
> Leg de Clean Architecture-laagverdeling uit voor dit project.

**Verwacht gedrag:**
Skill laadt NIET. Claude beantwoordt op basis van CLAUDE.md zonder migratiecontext.

---

### Test N3: Entiteit aanmaken zonder database-actie

**Prompt:**
> Maak de HikeLog-klasse aan in het domeinproject.

**Verwacht gedrag:**
Skill laadt NIET (er is nog geen migratie nodig — de entiteit bestaat nog niet in de DbContext).

---

## Activatie controleren

Gebruik `/context` in Claude Code na het sturen van de prompt om te zien welke skills geladen zijn.
Als `dotnet-ef-migration` zichtbaar is onder de geladen skills, is de test geslaagd.
