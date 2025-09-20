# Proyecto: Agente con Cono de Visión (IA)

Este proyecto es una entrega para la materia de Inteligencia Artificial e Ingeniería del Conocimiento. Consiste en un agente controlado por IA que utiliza un cono de visión para detectar y perseguir a un objetivo en un entorno 3D, implementado en Unity.

---

## 🛠️ Características Principales

* **Detección por Cono de Visión:** El agente detecta objetivos dentro de un rango y ángulo de visión configurables.
* **Filtro por Capas (Layers):** La detección solo se activa para objetos en capas específicas (ej. "Enemy").
* **Línea de Visión:** El agente comprueba que no haya obstáculos entre él y el objetivo antes de iniciar la persecución.
* **Persecución Autónoma:** Utiliza `NavMeshAgent` de Unity para el comportamiento de persecución (Steering Behavior).
* **Visualización en Tiempo Real:** El cono de visión se dibuja en la escena y cambia de color al detectar un objetivo.

---

## ✒️ Autores

* Carlos Hernán González
* Cesar Sasia Zayas
