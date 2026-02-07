# AGENTS.md - Contexto y Reglas para LanzaTuIdea

Este archivo contiene contexto crítico, convenciones y comandos de configuración para agentes de IA que trabajan en el repositorio "LanzaTuIdea".

## 1. Resumen del Proyecto
**LanzaTuIdea** es una aplicación ASP.NET Core (.NET 8) que gestiona la presentación y seguimiento de ideas dentro de una organización. Utiliza una arquitectura monolítica con Controladores, Servicios y Entity Framework Core.

## 2. Pila Tecnológica
* **Framework:** .NET 8.0 (ASP.NET Core Web API + MVC).
* **Base de Datos:** SQL Server (vía Entity Framework Core).
* **ORM:** EF Core Code-First.
* **Frontend:** Vistas Razor / HTML estático en `wwwroot`.
* **Autenticación:** Cookie-based (`CookieAuthenticationDefaults`).

## 3. ⚠️ Reglas Críticas del Código (NO ROMPER)

### Autenticación "Modo Ninja" (Mock vs Real)
El proyecto utiliza una carga dinámica de servicios para desarrollo local que **NO DEBE SER REFACTORIZADA**.
* En `Program.cs`, el código busca la clase `LanzaTuIdea.Api.Services.MockAdServiceClient` usando `Type.GetType()`.
* **Contexto:** Este archivo suele estar en `.gitignore` y solo existe en las máquinas locales de los desarrolladores humanos.
* **Instrucción:** Si no encuentras el archivo `MockAdServiceClient.cs`, **NO intentes crearlo, borrar la lógica de reflexión ni reportarlo como error**. Asume que en el entorno de CI/CD, Producción o Sandbox se usará la implementación real `AdServiceClient`.

### Puertos y URLs
* El puerto predeterminado para desarrollo es **http://localhost:5116**.
* Swagger está disponible en `/swagger`.
* Si el entorno requiere un puerto específico (ej. 8080), usa `dotnet run --urls "http://0.0.0.0:8080"`.

## 4. Configuración del Entorno (Sandbox / CI / Codespaces)
**IMPORTANTE:** Si al ejecutar `dotnet --version` obtienes un error o "command not found", **DEBES** ejecutar el siguiente script de configuración inmediatamente. Este script instala el SDK, configura las rutas persistentes y restaura las herramientas necesarias.

### Setup Script (Ejecutar en Bash)
```bash
#!/usr/bin/env bash
# Script de configuración híbrido: Robustez + Herramientas EF
set -euo pipefail

# 1. Instalar .NET SDK 8.0 solo si no existe
if ! command -v dotnet >/dev/null 2>&1; then
    echo "🔍 .NET no detectado. Iniciando instalación..."
    
    if command -v wget >/dev/null 2>&1; then
        wget [https://dot.net/v1/dotnet-install.sh](https://dot.net/v1/dotnet-install.sh) -O dotnet-install.sh
    else
        curl -sSL [https://dot.net/v1/dotnet-install.sh](https://dot.net/v1/dotnet-install.sh) -o dotnet-install.sh
    fi
    
    chmod +x ./dotnet-install.sh
    ./dotnet-install.sh --channel 8.0 --install-dir "$HOME/.dotnet"

    # Configurar variables para ESTA sesión
    export DOTNET_ROOT="$HOME/.dotnet"
    export PATH="$DOTNET_ROOT:$DOTNET_ROOT/tools:$PATH"

    # Persistir variables para futuras sesiones del agente
    if ! grep -q 'DOTNET_ROOT' "$HOME/.bashrc"; then
        echo '' >> "$HOME/.bashrc"
        echo '# Configuración .NET para LanzaTuIdea' >> "$HOME/.bashrc"
        echo 'export DOTNET_ROOT="$HOME/.dotnet"' >> "$HOME/.bashrc"
        echo 'export PATH="$DOTNET_ROOT:$DOTNET_ROOT/tools:$PATH"' >> "$HOME/.bashrc"
    fi
    echo "✅ .NET SDK Instalado."
else
    echo "⚡ .NET SDK ya estaba instalado."
fi

# 2. Instalar herramienta de Entity Framework (CRÍTICO para migraciones)
echo "🛠 Verificando herramientas EF Core..."
dotnet tool install --global dotnet-ef || echo "Nota: dotnet-ef ya estaba instalado."

# 3. Restaurar dependencias del proyecto
echo "📦 Restaurando paquetes NuGet..."
PROJECT_FILE=$(find . -name "*.csproj" -print -quit)

if [ -n "$PROJECT_FILE" ]; then
    echo "   Encontrado proyecto: $PROJECT_FILE"
    dotnet restore "$PROJECT_FILE"
else
    echo "⚠️ ADVERTENCIA: No se encontró archivo .csproj automáticamente. Busca manualmante."
fi

echo "🚀 Setup completado. Ya puedes compilar y ejecutar."
