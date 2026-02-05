# LanzaTuIdea (INFOTEP) - MVP Auth + Roles

## Requisitos
- .NET 8 SDK
- SQL Server (instancia local) o SQL Server LocalDB para desarrollo

## Configuración rápida
1. Ajusta el endpoint del servicio SOAP si aplica:
   - `appsettings.json` → `AdService:BaseUrl`
2. (Opcional) agrega un seed de empleados:
   - Copia `seed/empleados.sample.csv` a `seed/empleados.csv` y completa los datos.
3. (Opcional) define bootstrap admins en desarrollo:
   - `BootstrapAdmins: ["usuario1", "usuario2"]`

## Arquitectura de Datos: Empleados (Dev vs Prod)

La aplicación implementa una estrategia híbrida para consultar la información de los empleados, permitiendo aislar el entorno de desarrollo de los datos reales de producción (SPN).

### Comportamiento
* **En Desarrollo (`Development`):** La aplicación utiliza una tabla física local llamada `Employees`. Esta tabla es gestionada por Entity Framework y se alimenta automáticamente desde el archivo `seed/empleados.csv` si está vacía.
* **En Producción:** La aplicación ignora la tabla física y mapea la entidad `Employee` directamente a una Vista SQL llamada `vw_Employees`. Esta vista debe existir en la base de datos de producción y apuntar a la fuente real de datos (SPN).

### Cómo activar el modo Producción (Desactivar tabla local)
Para que la aplicación deje de usar la tabla local y comience a consumir la Vista de producción, se debe cambiar la configuración en el `appsettings.json` o mediante variables de entorno en el servidor.

**Configuración en `appsettings.json`:**

```json
"Database": {
  "AutoMigrate": false,
  "UseEmployeeView": true
}
```

Nota para TI/DBA: Antes de activar UseEmployeeView: true, asegúrese de que la vista vw_Employees exista en la base de datos de destino y tenga las mismas columnas que el modelo de datos de la aplicación.

## Cómo correr en desarrollo
```bash
dotnet run
```

Si no tienes LocalDB instalado, ajusta `ConnectionStrings:DefaultConnection` en `appsettings.json` para apuntar a tu instancia local de SQL Server.

En Development la app:
- Aplica migraciones automáticamente.
- Crea roles base (Admin, Ideador).
- Si existe `seed/empleados.csv`, lo importa cuando la tabla está vacía.

## 🔐 Configuración de Seguridad (Token AD)
El token de AD no se incluye en el repositorio por motivos de seguridad. En desarrollo debes configurarlo usando .NET User Secrets:

```bash
# 1. Inicializar los secretos de usuario (si no se ha hecho)
dotnet user-secrets init

# 2. Establecer el token (Reemplazar con el valor real provisto por TI)
dotnet user-secrets set "AdService:Token" "VALOR_DEL_TOKEN_AQUI"
```

Para Producción, se debe configurar la variable de entorno: `AdService__Token`.

## Flujos principales
- **Login**: usa el servicio SOAP real.
- **Ideador**: registra ideas y consulta su historial.
- **Admin** (solo si tiene rol Admin): revisa ideas, clasifica y registra manualmente.

## ⚙️ Configuración y Despliegue (Producción)

La aplicación tiene comportamientos distintos dependiendo del entorno (`Development` vs `Production`) y de la configuración en `appsettings.json`.

### 1. Migraciones de Base de Datos
* **Desarrollo:** Las migraciones se aplican automáticamente al iniciar la app (`dotnet run`).
* **Producción:** Por seguridad, las migraciones automáticas están **DESACTIVADAS** por defecto para evitar bloqueos o cambios accidentales en la BD productiva.

**Para activar migraciones automáticas en producción:**
Cambiar `AutoMigrate` a `true` en `appsettings.json`:
```json
"Database": {
  "AutoMigrate": true
}
```

## Checklist de pruebas manuales
1. Inicia sesión con un usuario válido.
2. Verifica que aparece la vista de ideador y el empleado está precargado.
3. Registra una idea y revisa la lista “Mis Ideas Lanzadas”.
4. (Admin) Accede al dashboard y a los inbox de ideas pendientes y revisadas.
5. (Admin) Actualiza estatus/clasificación desde el modal.
6. (Admin) Registra una idea manual y confirma que aparece en el dashboard.
