# Carte des sujets

Le grand arbre de QuizzWare : chaque catégorie a un **tronc commun** (chapitres) et un **arbre de spécialisations**.
Fichier généré par `tools/GenerateurContenu` à partir du plan et du contenu : ne pas modifier à la main.

✅ écrit · 🟡 en partie écrit · 🕓 à venir · ↪ renvoi vers le parcours d'une autre catégorie · 🚪 porte vers une autre catégorie

| Catégories | Chapitres de tronc commun | Leçons de tronc commun | Spécialisations | Renvois | Portes |
|---|---|---|---|---|---|
| 20 | 133 (133 écrits) | 341 (341 écrites) | 185 (7 écrites) | 28 | 41 |

## Architecture système

**Tronc commun**

1. ✅ Principes de conception
   - ✅ Les principes SOLID
   - ✅ Couplage, cohésion et simplicité
2. ✅ Design patterns
   - ✅ Patterns de création
   - ✅ Patterns de structure
   - ✅ Patterns de comportement
3. ✅ Architecture applicative
   - ✅ Architecture en couches
   - ✅ Hexagonale et clean architecture
   - ✅ Les bases du DDD
4. ✅ Concevoir des APIs
   - ✅ REST en pratique
   - ✅ Versioning et compatibilité
   - ✅ Idempotence et nouvelles tentatives
5. ✅ Données à l'échelle
   - ✅ Stratégies de cache
   - ✅ CAP et PACELC
   - ✅ Partitionnement
6. ✅ Systèmes distribués
   - ✅ Monolithe, microservices et systèmes distribués
   - ✅ Découper en services
7. ✅ Communication asynchrone
   - ✅ Files et topics
   - ✅ Garanties de livraison
   - ✅ Outbox et idempotence
8. ✅ Résilience et observabilité
   - ✅ Timeouts, retries et circuit breakers
   - ✅ Limitation de débit
   - ✅ Logs, métriques et traces

**Spécialisations**

- Conception logicielle
  - 🕓 Design patterns
  - 🕓 Clean architecture et hexagonale
  - 🕓 Domain-Driven Design
  - 🕓 Refactoring et code legacy
- APIs
  - 🕓 REST
  - 🕓 gRPC
  - 🕓 GraphQL
  - 🕓 OpenAPI et contrats
- Systèmes distribués
  - 🕓 Kafka
  - 🕓 RabbitMQ
  - 🕓 Event sourcing et CQRS
  - 🕓 Workflows durables
- Architecture cloud
  - 🕓 Serverless
  - 🕓 Multi-régions
- Observabilité
  - 🕓 OpenTelemetry
  - ↪ Prometheus et Grafana → Architecture infra
- System design d'entretien
  - ✅ Démarche et estimations
  - 🕓 Études de cas
- Autres horizons
  - 🚪 Sécurité applicative → catégorie Sécurité applicative
  - 🚪 Algorithmes et structures de données → catégorie Algorithmes et structures de données
  - 🚪 Tests et qualité → catégorie Tests et qualité

## Architecture infra

**Tronc commun**

1. ✅ Linux et shell
   - ✅ Processus et services
   - ✅ Fichiers et permissions
   - ✅ Le shell
2. ✅ Conteneurs
   - ✅ Conteneurs et Docker
   - ✅ Conteneurs en pratique
3. ✅ Orchestration et scalabilité
   - ✅ Kubernetes et scalabilité
   - ✅ Manifestes et ressources
4. ✅ CI/CD
   - ✅ Pipelines d'intégration continue
   - ✅ Stratégies de déploiement
   - ✅ GitOps
5. ✅ Infrastructure as Code
   - ✅ Principes de l'IaC
   - ✅ Terraform en bref
   - ✅ Gestion de configuration
6. ✅ Le cloud
   - ✅ IaaS, PaaS, SaaS, serverless
   - ✅ Réseau cloud
   - ✅ Identités et accès
7. ✅ Observabilité et SRE
   - ✅ Supervision et alertes
   - ✅ SLI, SLO et budgets d'erreur
   - ✅ Gestion d'incident
8. ✅ Sécurité de l'infrastructure
   - ✅ Gestion des secrets
   - ✅ Chaîne d'approvisionnement

**Spécialisations**

- Conteneurs et orchestration
  - 🕓 Docker
  - ✅ Kubernetes
  - 🕓 Helm
  - 🕓 Service mesh
- Cloud public
  - 🕓 AWS
  - 🕓 Azure
  - 🕓 Google Cloud
- Infrastructure as Code
  - 🕓 Terraform
  - 🕓 Ansible
  - 🕓 Pulumi
- CI/CD
  - 🕓 GitHub Actions
  - 🕓 GitLab CI
  - 🕓 Argo CD et GitOps
- Observabilité et SRE
  - 🕓 Prometheus et Grafana
  - 🕓 Centralisation des logs
  - 🕓 SLI, SLO et gestion d'incident
- Systèmes Linux
  - 🕓 Administration Linux
  - 🕓 Bash et scripts
  - 🕓 Nginx
- Platform engineering
  - 🕓 Portails développeurs
  - 🕓 FinOps
- Autres horizons
  - 🚪 Systèmes d'exploitation → catégorie Systèmes d'exploitation
  - 🚪 Sécurité applicative → catégorie Sécurité applicative
  - 🚪 Git et collaboration → catégorie Git et collaboration

## Réseau

**Tronc commun**

1. ✅ Fondamentaux des réseaux
   - ✅ Modèle OSI et TCP/IP
   - ✅ Encapsulation, adresses et ports
2. ✅ Adressage IP
   - ✅ IPv4 et CIDR
   - ✅ IPv6
   - ✅ NAT et adresses privées
3. ✅ TCP et UDP en profondeur
   - ✅ Cycle de vie d'une connexion TCP
   - ✅ Contrôle de flux et de congestion
   - ✅ UDP et QUIC
4. ✅ Le web : DNS, HTTP et TLS
   - ✅ DNS, HTTP et adressage IP
   - ✅ HTTP en détail
5. ✅ Routage et commutation
   - ✅ Commutation et VLAN
   - ✅ Routage IP
   - ✅ Internet et BGP
6. ✅ Infrastructure réseau des applications
   - ✅ Répartition de charge
   - ✅ Proxys et reverse proxys
   - ✅ CDN
7. ✅ Sécurité réseau
   - ✅ TLS en détail
   - ✅ Pare-feu et segmentation
   - ✅ Attaques courantes
8. ✅ Diagnostic réseau
   - ✅ Les outils de base
   - ✅ Lire une capture

**Spécialisations**

- Protocoles du web
  - 🕓 HTTP/2 et HTTP/3
  - 🕓 WebSocket et temps réel
  - ↪ gRPC → Architecture système
- Sécurité réseau
  - 🕓 TLS et PKI
  - 🕓 Pare-feu et VPN
  - 🕓 Attaques réseau
- Réseau d'infrastructure
  - 🕓 Réseau cloud
  - ↪ Nginx : reverse proxy et load balancing → Architecture infra
  - 🕓 CDN et edge
  - 🕓 DNS avancé
- Réseaux sans fil et mobiles
  - 🕓 Wi-Fi
  - 🕓 Réseaux cellulaires
- Diagnostic
  - 🕓 Wireshark et tcpdump
  - 🕓 curl, dig et traceroute
- Programmation réseau
  - 🕓 Sockets
- Autres horizons
  - 🚪 Sécurité applicative → catégorie Sécurité applicative
  - 🚪 Systèmes d'exploitation → catégorie Systèmes d'exploitation
  - 🚪 Architecture infra → catégorie Architecture infra

## C++

**Tronc commun**

1. ✅ Les fondamentaux
   - ✅ Types, variables et constantes
   - ✅ Contrôle du flux
   - ✅ Fonctions
2. ✅ Références et pointeurs
   - ✅ Les références
   - ✅ Les pointeurs
   - ✅ const, pointeurs et tableaux
3. ✅ Classes et objets
   - ✅ Classes et encapsulation
   - ✅ Constructeurs et destructeur
   - ✅ Surcharge d'opérateurs
4. ✅ Mémoire et RAII
   - ✅ Pile, tas et new/delete
   - ✅ RAII et pointeurs intelligents
5. ✅ Copie et déplacement
   - ✅ Copie profonde et superficielle
   - ✅ Sémantique de déplacement
   - ✅ La règle du zéro
6. ✅ Héritage et polymorphisme
   - ✅ Héritage
   - ✅ Fonctions virtuelles
   - ✅ Pièges du polymorphisme
7. ✅ Templates
   - ✅ Templates de fonctions
   - ✅ Templates de classes
   - ✅ Concepts (C++20)
8. ✅ La bibliothèque standard
   - ✅ Conteneurs
   - ✅ Itérateurs et invalidation
   - ✅ Algorithmes
9. ✅ Erreurs et exceptions
   - ✅ Exceptions
   - ✅ Garanties d'exception
   - ✅ Alternatives aux exceptions
10. ✅ Concurrence
   - ✅ Threads
   - ✅ Mutex et verrous
   - ✅ Atomics
11. ✅ C++ moderne
   - ✅ Lambdas
   - ✅ constexpr et consteval
   - ✅ De C++17 à C++23
12. ✅ Pièges et comportements indéfinis
   - ✅ Comportements indéfinis
   - ✅ Pièges classiques d'entretien

**Spécialisations**

- Frameworks et bibliothèques
  - 🕓 Qt
  - 🕓 Boost
- Jeu vidéo et temps réel
  - 🕓 Unreal Engine
  - 🕓 Programmation graphique
- Performance
  - 🕓 Profilage et optimisation
  - 🕓 Concurrence avancée
  - 🕓 SIMD et vectorisation
- Systèmes embarqués
  - 🕓 C++ embarqué
- Domaines d'application
  - 🕓 Finance à basse latence
  - 🕓 Audio et temps réel
- Outillage
  - 🕓 CMake
  - 🕓 Sanitizers et débogage
  - 🕓 GoogleTest
- Autres horizons
  - 🚪 C → catégorie C
  - 🚪 Rust → catégorie Rust
  - 🚪 Algorithmes et structures de données → catégorie Algorithmes et structures de données

## C#

**Tronc commun**

1. ✅ Bases du langage
   - ✅ Types valeur et types référence
   - ✅ Chaînes et formatage
   - ✅ Méthodes et paramètres
2. ✅ Programmation orientée objet
   - ✅ Classes et propriétés
   - ✅ Héritage et interfaces
   - ✅ Records et structs
3. ✅ Collections et génériques
   - ✅ Les collections
   - ✅ Génériques
   - ✅ IEnumerable et yield
4. ✅ LINQ et asynchrone
   - ✅ LINQ
   - ✅ async et await
   - ✅ Pièges de l'asynchrone
5. ✅ Mémoire et performance
   - ✅ Le ramasse-miettes
   - ✅ IDisposable et using
   - ✅ Span et allocations
6. ✅ Exceptions et nullabilité
   - ✅ Exceptions
   - ✅ Nullable reference types
7. ✅ Délégués, événements et lambdas
   - ✅ Délégués, Func et Action
   - ✅ Événements
   - ✅ Lambdas et closures
8. ✅ C# moderne
   - ✅ Pattern matching
   - ✅ Nouveautés récentes
9. ✅ L'écosystème .NET
   - ✅ Runtime et compilation
   - ✅ Injection de dépendances

**Spécialisations**

- Web et API
  - ✅ ASP.NET Core
  - 🕓 Blazor
  - 🕓 SignalR
  - ↪ gRPC en .NET → Architecture système
- Accès aux données
  - ↪ Entity Framework Core → Bases de données
  - 🕓 Dapper
- Applications et jeux
  - 🕓 .NET MAUI
  - 🕓 WPF
  - 🕓 Unity
- .NET et cloud
  - 🕓 .NET Aspire
  - 🕓 Azure Functions
- Qualité et performance
  - 🕓 xUnit et tests
  - 🕓 Performance .NET
- Architecture .NET
  - ↪ Clean architecture et hexagonale → Architecture système
- Autres horizons
  - 🚪 Java → catégorie Java
  - 🚪 Développement mobile → catégorie Développement mobile
  - 🚪 Tests et qualité → catégorie Tests et qualité

## C

**Tronc commun**

1. ✅ Bases du langage
   - ✅ Types et tailles
   - ✅ Opérateurs et promotions
   - ✅ Contrôle et fonctions
2. ✅ Pointeurs et mémoire
   - ✅ Pointeurs et mémoire
   - ✅ Tableaux, pointeurs et const
3. ✅ Chaînes, préprocesseur et mots-clés
   - ✅ Chaînes, préprocesseur et mots-clés
   - ✅ Manipuler des chaînes
4. ✅ Mémoire dynamique
   - ✅ Allouer et libérer
   - ✅ realloc et tableaux dynamiques
   - ✅ Fuites et double free
5. ✅ Structures de données
   - ✅ Structures
   - ✅ Unions et énumérations
   - ✅ Listes chaînées
6. ✅ Compilation et modules
   - ✅ Headers et unités de compilation
   - ✅ static, extern et linkage
   - ✅ Make
7. ✅ Entrées / sorties
   - ✅ stdio
   - ✅ Fichiers
8. ✅ Comportements indéfinis et sécurité
   - ✅ Comportements indéfinis courants
   - ✅ Débordements de tampon
9. ✅ Bas niveau
   - ✅ Opérations bit à bit
   - ✅ Endianness

**Spécialisations**

- Programmation système
  - 🕓 API POSIX
  - 🕓 Noyau Linux
- Systèmes embarqués
  - 🕓 Microcontrôleurs
  - 🕓 FreeRTOS
  - 🕓 Zephyr
- Sécurité
  - 🕓 Failles mémoire et défenses
  - 🕓 Règles de code sûr
- Projets classiques
  - 🕓 Écrire un interpréteur
  - 🕓 Écrire un émulateur
- Outillage
  - 🕓 GCC, Clang et Make
  - 🕓 GDB et Valgrind
- Autres horizons
  - 🚪 C++ → catégorie C++
  - 🚪 Rust → catégorie Rust
  - 🚪 Systèmes d'exploitation → catégorie Systèmes d'exploitation

## Python

**Tronc commun**

1. ✅ Bases du langage
   - ✅ Types, variables et mutabilité
   - ✅ Contrôle du flux
   - ✅ Chaînes et f-strings
2. ✅ Structures de données
   - ✅ Listes et tuples
   - ✅ Dictionnaires et ensembles
   - ✅ Le module collections
3. ✅ Fonctions
   - ✅ Arguments
   - ✅ Portée (LEGB)
   - ✅ Closures
4. ✅ Programmation orientée objet
   - ✅ Classes
   - ✅ Méthodes spéciales
   - ✅ Dataclasses et héritage
5. ✅ Itérateurs et générateurs
   - ✅ Le protocole d'itération
   - ✅ Générateurs
6. ✅ Fonctions avancées et concurrence
   - ✅ Décorateurs et gestionnaires de contexte
   - ✅ Threads, processus et GIL
   - ✅ asyncio
7. ✅ Erreurs et tests
   - ✅ Exceptions
   - ✅ Tests avec pytest
8. ✅ Typage et outillage
   - ✅ Type hints
   - ✅ Environnements et paquets
9. ✅ Fonctionnement interne
   - ✅ CPython
   - ✅ Gestion de la mémoire

**Spécialisations**

- Développement web
  - 🕓 Django
  - ✅ FastAPI
  - 🕓 Flask
- Data et IA
  - 🕓 NumPy
  - 🕓 pandas
  - 🕓 Polars
  - 🕓 scikit-learn
  - 🕓 PyTorch
- Accès aux données
  - ↪ SQLAlchemy → Bases de données
- Asynchrone
  - 🕓 asyncio en profondeur
- Automatisation
  - 🕓 Scripts et CLI
  - 🕓 HTTP et scraping
- Qualité et outillage
  - 🕓 pytest
  - 🕓 Typage et mypy
  - 🕓 Packaging
- Autres horizons
  - 🚪 Intelligence artificielle → catégorie Intelligence artificielle
  - 🚪 Algorithmes et structures de données → catégorie Algorithmes et structures de données
  - 🚪 Tests et qualité → catégorie Tests et qualité

## Bases de données

**Tronc commun**

1. ✅ Premiers pas en SQL
   - ✅ Tables et SELECT
   - ✅ NULL et la logique à trois valeurs
   - ✅ Clés et contraintes
2. ✅ Le panorama des bases de données
   - ✅ Relationnel et NoSQL
   - ✅ Document, clé-valeur, colonnes, graphe
   - ✅ Les bases spécialisées
3. ✅ Jointures et agrégats
   - ✅ Les jointures
   - ✅ GROUP BY et agrégats
4. ✅ Modélisation et normalisation
   - ✅ Modéliser les relations
   - ✅ La normalisation
5. ✅ Index et performance
   - ✅ Comment fonctionne un index
   - ✅ Lire un plan et éviter les pièges
6. ✅ Transactions et concurrence
   - ✅ Transactions et ACID
   - ✅ Les niveaux d'isolation
   - ✅ Verrous et deadlocks
7. ✅ SQL avancé
   - ✅ Sous-requêtes et CTE
   - ✅ Les fonctions de fenêtrage
8. ✅ Mise à l'échelle
   - ✅ Réplication et cache
   - ✅ Partitionnement et sharding

**Spécialisations**

- Bases relationnelles
  - ✅ PostgreSQL
  - 🕓 MySQL et MariaDB
  - 🕓 SQL Server
  - 🕓 SQLite
  - ✅ Entity Framework Core
  - 🕓 SQLAlchemy
- Bases document
  - ✅ MongoDB
- Clé-valeur
  - 🕓 Redis
  - 🕓 DynamoDB
- Colonnes larges
  - 🕓 Cassandra
- Graphe
  - 🕓 Neo4j
- Moteurs de recherche
  - 🕓 Elasticsearch
- Bases vectorielles
  - 🕓 pgvector
- Séries temporelles
  - 🕓 TimescaleDB
- Analytique
  - 🕓 DuckDB
  - 🕓 ClickHouse
- Flux de données
  - ↪ Kafka → Architecture système
  - 🕓 Capture des changements (CDC)
- Autres horizons
  - 🚪 Intelligence artificielle → catégorie Intelligence artificielle
  - 🚪 Algorithmes et structures de données → catégorie Algorithmes et structures de données

## Algorithmes et structures de données

**Tronc commun**

1. ✅ Complexité
   - ✅ La notation grand O
   - ✅ Analyser un algorithme
2. ✅ Structures linéaires
   - ✅ Tableaux dynamiques
   - ✅ Listes chaînées
   - ✅ Piles et files
3. ✅ Tables de hachage
   - ✅ Fonctionnement
   - ✅ Problèmes classiques
4. ✅ Arbres
   - ✅ Arbres binaires
   - ✅ Arbres binaires de recherche
   - ✅ Tas et files de priorité
5. ✅ Graphes
   - ✅ Représenter un graphe
   - ✅ BFS et DFS
   - ✅ Plus court chemin
6. ✅ Tri et recherche
   - ✅ Algorithmes de tri
   - ✅ Recherche binaire
7. ✅ Techniques de résolution
   - ✅ Deux pointeurs et fenêtre glissante
   - ✅ Récursion et backtracking
   - ✅ Programmation dynamique
   - ✅ Algorithmes gloutons

**Spécialisations**

- Préparation aux entretiens
  - 🕓 Problèmes par patterns
  - 🕓 Coder en direct
- Structures avancées
  - 🕓 Tries et arbres de suffixes
  - 🕓 Union-Find
  - 🕓 Arbres de segments
- Algorithmes par domaine
  - 🕓 Algorithmes sur les chaînes
  - 🕓 Géométrie algorithmique
  - 🕓 Structures probabilistes
- Autres horizons
  - 🚪 Intelligence artificielle → catégorie Intelligence artificielle
  - 🚪 Architecture système → catégorie Architecture système

## JavaScript et TypeScript

**Tronc commun**

1. ✅ Bases du langage
   - ✅ Types et coercition
   - ✅ let, const et portée
   - ✅ Fonctions
2. ✅ Objets et prototypes
   - ✅ Objets et déstructuration
   - ✅ Prototypes et classes
3. ✅ Asynchrone
   - ✅ La boucle d'événements
   - ✅ Promesses
   - ✅ async / await
4. ✅ TypeScript
   - ✅ Types de base
   - ✅ Types avancés
   - ✅ Narrowing
5. ✅ Modules et outillage
   - ✅ Modules
   - ✅ npm et les dépendances
6. ✅ Le navigateur
   - ✅ Le DOM
   - ✅ Événements

**Spécialisations**

- Frameworks frontend
  - 🕓 React
  - 🕓 Vue
  - 🕓 Angular
  - 🕓 Svelte
- JavaScript côté serveur
  - 🕓 Node.js
  - 🕓 Express et NestJS
  - 🕓 Deno et Bun
- Outillage
  - 🕓 Vite et les bundlers
  - 🕓 Vitest, Jest et Playwright
- Autres horizons
  - 🚪 Développement web frontend → catégorie Développement web frontend
  - 🚪 Développement mobile → catégorie Développement mobile

## Java

**Tronc commun**

1. ✅ Bases du langage
   - ✅ Types primitifs et objets
   - ✅ Classes et objets
   - ✅ Chaînes
2. ✅ Orienté objet
   - ✅ Héritage et interfaces
   - ✅ Records et sealed classes
3. ✅ Collections et génériques
   - ✅ Le framework Collections
   - ✅ equals et hashCode
   - ✅ Streams
4. ✅ Exceptions
   - ✅ Exceptions
   - ✅ Optional et null
5. ✅ Concurrence
   - ✅ Threads et synchronized
   - ✅ Executors et CompletableFuture
   - ✅ Virtual threads
6. ✅ La JVM
   - ✅ Mémoire et garbage collectors
   - ✅ Bytecode et JIT

**Spécialisations**

- Écosystème Spring
  - 🕓 Spring Boot
  - 🕓 Spring Data JPA
  - 🕓 Spring Security
- Accès aux données
  - 🕓 Hibernate
- Outillage
  - 🕓 Maven et Gradle
  - 🕓 JUnit et Mockito
- Langages de la JVM
  - 🕓 Kotlin
  - 🕓 Scala
- Autres horizons
  - 🚪 Développement mobile → catégorie Développement mobile
  - 🚪 C# → catégorie C#

## Go

**Tronc commun**

1. ✅ Bases du langage
   - ✅ Types et variables
   - ✅ Fonctions et erreurs
   - ✅ Slices et maps
2. ✅ Structs et interfaces
   - ✅ Structs et méthodes
   - ✅ Interfaces
   - ✅ Génériques
3. ✅ Concurrence
   - ✅ Goroutines
   - ✅ Channels et select
   - ✅ sync et context
4. ✅ Outillage et tests
   - ✅ Modules
   - ✅ Tests et benchmarks

**Spécialisations**

- Services web
  - 🕓 net/http
  - 🕓 Gin
  - ↪ gRPC en Go → Architecture système
- Outillage cloud
  - 🕓 Outils en ligne de commande
  - 🕓 Opérateurs Kubernetes
- Autres horizons
  - 🚪 Architecture infra → catégorie Architecture infra
  - 🚪 Rust → catégorie Rust

## Rust

**Tronc commun**

1. ✅ Bases du langage
   - ✅ Types et variables
   - ✅ Contrôle et pattern matching
2. ✅ Ownership et emprunts
   - ✅ Ownership
   - ✅ Références et emprunts
   - ✅ Durées de vie
3. ✅ Structs, enums et traits
   - ✅ Structs et enums
   - ✅ Traits
4. ✅ Gestion des erreurs
   - ✅ Result et ?
   - ✅ panic! et unwrap
5. ✅ Concurrence
   - ✅ Threads, Arc et Mutex
   - ✅ async et Tokio

**Spécialisations**

- Web et services
  - 🕓 Axum
  - 🕓 Tokio
- Programmation système
  - 🕓 Rust embarqué
  - 🕓 WebAssembly
- Rust avancé
  - 🕓 unsafe et FFI
  - 🕓 Macros
- Autres horizons
  - 🚪 C++ → catégorie C++
  - 🚪 Systèmes d'exploitation → catégorie Systèmes d'exploitation

## Sécurité applicative

**Tronc commun**

1. ✅ Fondamentaux
   - ✅ Confidentialité, intégrité, disponibilité
   - ✅ Principes de conception sûre
   - ✅ Modélisation des menaces
2. ✅ Vulnérabilités web
   - ✅ Injections
   - ✅ XSS
   - ✅ CSRF et SSRF
3. ✅ Authentification et autorisation
   - ✅ Mots de passe
   - ✅ Sessions et JWT
   - ✅ OAuth 2 et OpenID Connect
4. ✅ Cryptographie appliquée
   - ✅ Chiffrement symétrique et asymétrique
   - ✅ Hachage et signatures
5. ✅ Sécurité dans le cycle de développement
   - ✅ Dépendances et supply chain
   - ✅ Analyses automatiques

**Spécialisations**

- Sécurité offensive
  - 🕓 Tests d'intrusion
  - 🕓 CTF
- Sécurité défensive
  - 🕓 Détection et réponse
  - 🕓 Investigation numérique
- Identités
  - 🕓 Keycloak
  - 🕓 OAuth et OIDC en profondeur
- Sécurité par domaine
  - ↪ TLS et PKI → Réseau
  - ↪ Failles mémoire et défenses → C
- Autres horizons
  - 🚪 Réseau → catégorie Réseau
  - 🚪 Architecture infra → catégorie Architecture infra

## Systèmes d'exploitation

**Tronc commun**

1. ✅ Processus et threads
   - ✅ Processus
   - ✅ Threads
   - ✅ Ordonnancement
2. ✅ Mémoire
   - ✅ Mémoire virtuelle
   - ✅ Pagination et TLB
   - ✅ Allocation mémoire
3. ✅ Synchronisation
   - ✅ Sections critiques
   - ✅ Primitives de synchronisation
   - ✅ Interblocages
4. ✅ Systèmes de fichiers
   - ✅ Fichiers et inodes
   - ✅ Journalisation
5. ✅ Entrées / sorties
   - ✅ Interruptions et appels système
   - ✅ E/S asynchrones

**Spécialisations**

- Linux
  - ↪ Noyau Linux → C
  - ↪ Administration Linux → Architecture infra
- Virtualisation
  - 🕓 Hyperviseurs
  - 🕓 Namespaces et cgroups
- Systèmes temps réel
  - ↪ FreeRTOS → C
- Autres horizons
  - 🚪 C → catégorie C
  - 🚪 Architecture système → catégorie Architecture système

## Git et collaboration

**Tronc commun**

1. ✅ Les bases de Git
   - ✅ Le modèle de Git
   - ✅ Commandes essentielles
2. ✅ Branches et fusions
   - ✅ Branches
   - ✅ merge ou rebase
   - ✅ Résoudre les conflits
3. ✅ Travail à distance
   - ✅ Remotes
   - ✅ Pull requests et revue de code
4. ✅ Git avancé
   - ✅ reset, revert et reflog
   - ✅ Rebase interactif
   - ✅ bisect
5. ✅ Workflows d'équipe
   - ✅ Stratégies de branches
   - ✅ Conventions

**Spécialisations**

- Plateformes
  - 🕓 GitHub
  - 🕓 GitLab
- Automatisation
  - ↪ GitHub Actions → Architecture infra
  - ↪ GitLab CI → Architecture infra
- Grands dépôts
  - 🕓 Monorepos
  - 🕓 Git LFS

## Tests et qualité

**Tronc commun**

1. ✅ Fondamentaux des tests
   - ✅ La pyramide des tests
   - ✅ Un bon test
2. ✅ Tests unitaires
   - ✅ Arrange, Act, Assert
   - ✅ Doublures de test
   - ✅ TDD
3. ✅ Tests d'intégration et de bout en bout
   - ✅ Tests d'intégration
   - ✅ Tests de bout en bout
4. ✅ Qualité du code
   - ✅ Code lisible
   - ✅ Revue de code
   - ✅ Refactoring
5. ✅ Tests avancés
   - ✅ Tests par propriétés
   - ✅ Tests de mutation
   - ✅ Tests de charge

**Spécialisations**

- Frameworks de test
  - ↪ xUnit (C#) → C#
  - ↪ pytest (Python) → Python
  - ↪ JUnit (Java) → Java
  - ↪ GoogleTest (C++) → C++
- Tests de bout en bout
  - 🕓 Playwright
  - 🕓 Cypress
- Performance
  - 🕓 k6
- Pratiques
  - 🕓 BDD
  - 🕓 Tests de contrat

## Développement web frontend

**Tronc commun**

1. ✅ HTML
   - ✅ HTML sémantique
   - ✅ Formulaires
2. ✅ CSS
   - ✅ Cascade et spécificité
   - ✅ Flexbox et Grid
   - ✅ Responsive design
3. ✅ Accessibilité
   - ✅ Principes WCAG
   - ✅ ARIA et navigation au clavier
4. ✅ Performance web
   - ✅ Le rendu du navigateur
   - ✅ Core Web Vitals
5. ✅ Architecture frontend
   - ✅ CSR, SSR et SSG
   - ✅ Gestion de l'état

**Spécialisations**

- Frameworks
  - ↪ React → JavaScript et TypeScript
  - ↪ Vue → JavaScript et TypeScript
  - ↪ Angular → JavaScript et TypeScript
- Méta-frameworks
  - 🕓 Next.js
  - 🕓 Nuxt
- Outils CSS
  - 🕓 Tailwind CSS
  - 🕓 Sass
- Design et UX
  - 🕓 Design systems
- Autres horizons
  - 🚪 JavaScript et TypeScript → catégorie JavaScript et TypeScript
  - 🚪 Développement mobile → catégorie Développement mobile

## Développement mobile

**Tronc commun**

1. ✅ Fondamentaux
   - ✅ Cycle de vie des applications
   - ✅ Contraintes mobiles
2. ✅ Interfaces mobiles
   - ✅ Navigation
   - ✅ Mises en page adaptatives
3. ✅ Données et réseau
   - ✅ Stockage local
   - ✅ Mode hors ligne
4. ✅ Publication
   - ✅ Signature et stores
   - ✅ CI/CD mobile

**Spécialisations**

- Natif
  - 🕓 Android et Kotlin
  - 🕓 iOS et Swift
- Multiplateforme
  - 🕓 Flutter
  - 🕓 React Native
  - ↪ .NET MAUI → C#
- Services mobiles
  - 🕓 Notifications push
  - 🕓 Firebase

## Intelligence artificielle

**Tronc commun**

1. ✅ Fondamentaux du machine learning
   - ✅ Types d'apprentissage
   - ✅ Préparer les données
   - ✅ Évaluer un modèle
2. ✅ Modèles classiques
   - ✅ Régression linéaire et logistique
   - ✅ Arbres et forêts
   - ✅ Clustering
3. ✅ Deep learning
   - ✅ Réseaux de neurones
   - ✅ Architectures classiques
   - ✅ Transformers
4. ✅ Les LLM dans les applications
   - ✅ Prompting
   - ✅ RAG
   - ✅ Agents et outils
5. ✅ L'IA en production
   - ✅ MLOps
   - ✅ Évaluer des LLM

**Spécialisations**

- Frameworks
  - ↪ PyTorch → Python
  - ↪ scikit-learn → Python
- LLM
  - 🕓 APIs de LLM
  - 🕓 RAG en profondeur
  - ↪ pgvector → Bases de données
- Domaines
  - 🕓 Vision par ordinateur
  - 🕓 Traitement du langage
- Données
  - ↪ pandas → Python
  - 🕓 Data engineering
- Autres horizons
  - 🚪 Python → catégorie Python
  - 🚪 Algorithmes et structures de données → catégorie Algorithmes et structures de données

