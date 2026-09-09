# NexusWorkspace — Revisión de coherencia y plan de arreglo

> Fecha: 2026-09-08. Escrito tras la primera prueba real de la app.
> **Veredicto:** el modelo de datos y la capa de aplicación están ~90 % completos y
> bien diseñados. El problema es casi todo de **capa de UI** (qué pantalla deja
> hacer qué) más **3–4 decisiones de diseño** sobre conceptos que se solapan.
> No hace falta reescribir nada del dominio. Es trabajo acotado, no un rediseño.

---

## 1. Editar: existe el servicio, falta la pantalla

Todo esto ya funciona en `*Service.UpdateDetailsAsync` / joins del modelo; solo
falta el formulario.

| Pantalla | Hoy no deja | Backend que ya existe |
|---|---|---|
| **Tarea** | editar título, descripción, fecha inicio, **fecha límite con hora**, prioridad (sí), asignado, empresa relacionada; etiquetar; renombrar/borrar/reordenar subtareas y checklist | `WorkTaskService.UpdateDetailsAsync` (título, descr., prioridad, `DueDateUtc`, asignado, empresa). Falta `AddTag/RemoveTag` para tarea. |
| **Proyecto** | editar nombre, descripción, fechas, prioridad, responsable; etiquetar; vincular personas/empresas | `ProjectService.UpdateDetailsAsync`. Falta `AddTag` y métodos de vínculo. |
| **Persona** | asignarla a una empresa (empleador); vincularla a proyectos/tareas (equipo) | `Person.CompanyId`, joins `ProjectPerson`/`WorkTaskPerson` en el modelo, sin método de servicio ni UI. |
| **Empresa** | añadir una persona; vincular a proyectos; **botón "añadir etiqueta"** (solo está el de quitar) | join `ProjectCompany` en el modelo, sin método ni UI. |
| **Recordatorios** | fijar **hora y minutos** (solo fecha) | `RemindAtUtc` ya es `DateTime` completo; `ReminderService.RescheduleAsync` existe. Solo falta un TimePicker. |

**Acción:** panel de edición en Tarea y Proyecto (copiando el patrón que ya tienen
Persona y Empresa: `IsEditPanelOpen` → formulario → `SaveDetailsAsync`), editor de
chips de etiquetas en las 4 pantallas, y "picker de vínculo" (añadir persona/empresa
relacionada) en cada detalle.

---

## 2. Conceptos que se pisan — decisiones de diseño

### 2.1 Subtareas vs Checklist  → **fusionar en "Subtareas"**
Hoy son lo mismo: lista de cosas pequeñas dentro de una tarea. Diferencias reales:
subtarea es anidable y tiene `CompletedAtUtc`; checklist es plana. Ninguna se puede
editar (solo añadir y marcar).

**Propuesta:** una sola lista, **Subtareas**, con:
- añadir rápido (una línea, como el checklist de ahora),
- anidar opcional (arrastrar bajo otra),
- renombrar, borrar, reordenar, marcar hecha,
- % de avance en la tarea (ya calculado).

Migración: mover cada `ChecklistItem` a `SubTask` (mismo `WorkTaskId`, `Text`→`Title`,
`IsChecked`→`IsDone`), migración EF de un solo sentido. El enum `EntityKind.ChecklistItem`
se deja (append-only) pero deja de usarse.

### 2.2 Acciones rápidas  → **renombrar a "Registrar evento" y adelgazar**
Hoy una acción rápida solo escribe una fila de actividad con un vocabulario fijo de
16 palabras y, en 2 casos, empuja el estado. Se solapa con:
- **Comunicaciones** — "Email enviado/recibido", "Llamada" *son* `CommunicationChannel`.
- **Reuniones** — "Reunión realizada".
- **Seguimientos** — "Pendiente cliente/proveedor" *es* el estado `WaitingClient/Provider`.
- **Cambio de estado** — "Incidencia resuelta" → `InProgress`.

**Propuesta:**
- "Email/Llamada/Reunión" en la tarea → **abren el compositor de Comunicación**
  (con persona/empresa precargada desde el asignado/empresa de la tarea).
- "Pendiente cliente/proveedor" → **es el cambio de estado** (que además abre el
  seguimiento, ver 2.3).
- Lo que sí es útil como registro de un clic — "Incidencia detectada/resuelta",
  "Desplegado DEV/PRE/PRO", "Prueba realizada", "Cambio solicitado", "Recordatorio
  enviado" — se queda como **"Registrar evento"**: añade una entrada de actividad
  tipada, y el usuario **puede definir sus propias etiquetas de evento** (tabla
  `TaskEventKind` pequeña, o un `Setting` JSON). Menos botones, con sentido.

### 2.3 Seguimientos  → **acoplarlos al estado de la tarea**
Hoy es una entidad paralela mal conectada. Los estados `WaitingClient` /
`WaitingProvider` / `Blocked` ya significan "esperando".

**Propuesta:**
- Al pasar una tarea a `WaitingClient/WaitingProvider/Blocked` → **ofrecer abrir un
  seguimiento** ligado a esa tarea, con "esperando a" precargado desde la empresa
  relacionada / el asignado, y el asunto = título de la tarea.
- Al volver a `InProgress/Finished` → **cerrar automáticamente** el seguimiento
  abierto de esa tarea (registrando la resolución).
- Mostrar el seguimiento **incrustado en la tarea** (contador de días, "enviar
  recordatorio", "registrar contacto") — ya casi está.
- Se sigue permitiendo un seguimiento suelto (algo que persigues sin tarea).
- Rellenar de verdad `WaitingOnPersonId/CompanyId` (los campos existen) con un picker.

### 2.4 Etiquetas  → **darles un uso: filtro transversal**
Existen, se crean/fusionan/fijan en su página, pero no se pueden poner desde el
detalle de tarea/proyecto.

**Propuesta:**
- Editor de chips en Tarea y Proyecto (y arreglar el de Empresa).
- Clic en una etiqueta en cualquier sitio → **lista filtrada** por esa etiqueta
  (tareas + proyectos + personas + empresas con ella).
- Las etiquetas fijadas aparecen como accesos en la barra lateral o el Dashboard.
- Sentido: categoría ligera y transversal ("facturación", "GDPR", "cliente-X").

### 2.5 Actividad  → **filtros + agrupar por día**
Hoy es un único chorro global sin filtros.

**Propuesta:**
- Filtros: por proyecto, tipo de entidad, tipo de actividad, actor, rango de fechas.
- Agrupar por día.
- La cronología **por entidad** (ya existe en proyecto/persona/empresa; **añadirla a
  la tarea**) es la vista principal; la global queda como auditoría con filtros.

### 2.6 Descripción de tarea vs Comentarios
El usuario mete la descripción en un comentario porque no hay campo.

**Propuesta:** campo **Descripción** de verdad en la tarea (multilínea, editable,
visible arriba). Los comentarios pasan a ser lo que deben: notas/discusión con fecha.

---

## 3. Vínculos entre entidades que faltan (el "no está relacionado")

El modelo tiene todas las tablas de unión; faltan método de servicio + UI:

- Proyecto ↔ Personas (equipo) · Proyecto ↔ Empresas (cliente / proveedores)
- Tarea ↔ Personas (colaboradores, además del único asignado) · Tarea ↔ Empresa
- Persona → Empresa (empleador)
- En cada detalle: botón "añadir relacionado…" con buscador que crea la unión real,
  además del `EntityRelation` genérico para cruces tipados (bloquea, duplica…).

---

## 4. Sección "Tareas" (hoy es un placeholder)

`PageKey.Tasks` = `IsImplemented: false`. No hay lista global de tareas.

**Propuesta:** `TasksViewModel` + vista: todas las tareas de todos los proyectos,
con filtros (proyecto, estado, prioridad, asignado, vencidas, con texto), orden,
abrir detalle, crear rápida. Quitar el placeholder del nav.

---

## 5. Orden de trabajo propuesto

| Bloque | Contenido | Tamaño |
|---|---|---|
| **A. Edición básica** ✅ | Panel de edición Tarea + Proyecto (incl. descripción, fechas con hora). Hora/min en recordatorios. Editor de etiquetas en Tarea/Proyecto. | M |
| **B1. Subtareas — lista unificada** ✅ | Subtareas y checklist se muestran y editan en UNA lista ("Subtareas"): marcar, renombrar en línea, borrar, subir/bajar. El "+ añadir" crea siempre una subtarea. Sin tocar BD. | M |
| **B2. Subtareas — fusión real** ⏳ | Migración EF `ChecklistItem`→`SubTask` + eliminar plumbing de checklist en Import/Plantillas/Seeder/DTOs. **Con el usuario delante** (toca datos reales). | M |
| **C. Vínculos** | Métodos de servicio + pickers: proyecto↔persona/empresa, tarea↔persona/empresa, persona→empresa. | M |
| **D. Seguimientos** | Acoplar a estado de tarea (abrir/cerrar auto), incrustar en tarea, picker de "esperando a". | M |
| **E. Acciones rápidas** | Colapsar en Comunicación/estado; dejar "Registrar evento" con tipos propios. | S |
| **F. Actividad** | Filtros + agrupar por día; cronología en la tarea. | S |
| **G. Página Tareas** | Lista global con filtros. | M |
| **H. Repaso final** | Recorrido diario completo; luego sí: icono, `build.bat installer`, docs, `main` + release. | — |

Nada de esto toca la arquitectura. Al terminar, la app hace el bucle diario de
verdad y *entonces* se publica la v1.

---

## 6. Pasada final de "uso real" (antes de la v1)

Petición del usuario (2026-09-08): además de que cada cosa funcione, la herramienta
tiene que **facilitar el trabajo diario**, con coherencia de principio a fin. Tras
los bloques A–G, una pasada centrada en el comportamiento, no en features nuevas:

- **Menos pasos para lo habitual.** Crear proyecto → tarea → ponerse a trabajar sin
  abrir 4 paneles. Acciones frecuentes a un clic o atajo; los paneles de edición
  con Enter para guardar y Esc para cerrar.
- **Un sitio para cada cosa, y solo uno.** Que no haya dos maneras de registrar lo
  mismo (comentario vs descripción vs comunicación vs acción rápida): cada concepto
  con un propósito claro y visible.
- **El estado manda.** Lo que el usuario ve al abrir la app debe responder "¿qué
  tengo hoy y qué está esperándome?" sin filtrar a mano: vencidas, en espera con
  días, recordatorios de hoy, seguimientos que tocan.
- **Coherencia visual y de vocabulario.** Mismos nombres, mismos colores de estado,
  mismos gestos (abrir detalle, editar, archivar) en todas las pantallas.
- **Nada que no sirva.** Si una sección o un botón no aporta al bucle diario, se
  quita o se fusiona. Mejor pocas cosas que se entienden que muchas a medias.
- **Errores y vacíos amables.** Estados vacíos que explican qué hacer; errores en
  lenguaje humano; confirmaciones solo cuando de verdad hay riesgo.

Se concreta en tareas cuando lleguemos, con la app ya usable.
