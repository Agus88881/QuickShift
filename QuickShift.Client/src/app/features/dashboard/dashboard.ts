import { Component, OnInit, ChangeDetectorRef } from '@angular/core';
import { Router } from '@angular/router';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { AuthService } from '../../core/services/auth'; 
import { JwtHelperService } from '@auth0/angular-jwt';
import { ShiftService } from '../../core/services/shift';

@Component({
  selector: 'app-dashboard',
  standalone: true,
  imports: [CommonModule, FormsModule],
  template: `
    <div style="padding: 20px; font-family: 'Segoe UI', Tahoma, Geneva, Verdana, sans-serif; max-width: 600px; margin: auto;">
      <h1 style="color: #2c3e50;">Panel de QuickShift</h1>
      
      <div style="background: #ecf0f1; padding: 15px; border-radius: 8px; margin-bottom: 20px; border-left: 5px solid #3498db;">
        <p><strong>Estado:</strong> Sesión Activa ✅</p>
        <p><strong>Empresa (Tenant):</strong> {{ tenantDisplayName }}</p>
        <p><strong>Tu Rol: </strong> <span style="text-transform: capitalize; font-weight: bold; color: #2980b9;">{{ userRole }}</span></p>
      </div>

      <div style="margin-bottom: 20px;">
        <button type="button"
                (click)="toggleShift()" 
                [disabled]="loadingShift"
                [style.background]="isWorking ? '#e67e22' : '#2ecc71'"
                style="color: white; border: none; padding: 15px; border-radius: 8px; cursor: pointer; width: 100%; font-weight: bold; font-size: 16px; transition: 0.3s;">
          {{ loadingShift ? 'Procesando...' : (isWorking ? '⏹️ Finalizar Jornada' : '▶️ Iniciar Jornada') }}
        </button>
      </div>

      <div *ngIf="userRole === 'Admin'" 
           style="background: #fff; border: 1px solid #ddd; padding: 20px; border-radius: 8px; margin-bottom: 20px; box-shadow: 0 2px 4px rgba(0,0,0,0.1);">
        <h3 style="margin-top: 0; color: #2c3e50;">Panel de Administración</h3>
        <p style="font-size: 14px; color: #7f8c8d;">Invitar a un nuevo miembro (email de Google):</p>
        
        <div style="display: flex; gap: 10px;">
          <input type="email" 
                 [(ngModel)]="inviteEmail" 
                 placeholder="ejemplo@gmail.com"
                 style="flex: 1; padding: 10px; border: 1px solid #bdc3c7; border-radius: 4px;">
          
          <button (click)="onInvite()" 
                  [disabled]="!inviteEmail"
                  style="background: #27ae60; color: white; border: none; padding: 10px 15px; border-radius: 4px; cursor: pointer;">
            Invitar
          </button>
        </div>
      </div>
      
      <button (click)="logout()" 
              style="background: #e74c3c; color: white; border: none; padding: 10px 20px; border-radius: 5px; cursor: pointer; width: 100%;">
        Cerrar Sesión
      </button>
    </div>
  `
})
export class DashboardComponent implements OnInit {
  private jwtHelper = new JwtHelperService();
  
  inviteEmail: string = '';
  userRole: string = 'User';
  tenantDisplayName: string = '';
  tenantId: number = 0;
  isWorking: boolean = false;
  loadingShift: boolean = false;

  constructor(
    private router: Router, 
    private authService: AuthService, 
    private shiftService: ShiftService,
    private cdr: ChangeDetectorRef 
  ) {}

  ngOnInit(): void {
    this.tenantDisplayName = localStorage.getItem('tenantName') || 'Desconocido';
    const savedWorkingStatus = localStorage.getItem('isWorking');
    this.isWorking = savedWorkingStatus === 'true';

    const token = localStorage.getItem('jwt_token');
    if (token) {
      const decodedToken = this.jwtHelper.decodeToken(token);
      this.userRole = decodedToken["role"] || decodedToken["http://schemas.microsoft.com/ws/2008/06/identity/claims/role"];
      this.tenantId = decodedToken["tenantId"];
    }
  }

  toggleShift() {
    this.loadingShift = true;
    
    if (this.isWorking) {
      this.shiftService.clockOut().subscribe({
        next: (res) => {
          this.isWorking = false;
          this.loadingShift = false;
          localStorage.setItem('isWorking', 'false');
          this.cdr.detectChanges(); 
          alert(`Turno cerrado. Horas trabajadas: ${res.hoursWorked}`);
        },
        error: (err) => {
          this.loadingShift = false;
          this.cdr.detectChanges();
          alert("Error al cerrar el turno");
        }
      });
    } else {
      this.shiftService.clockIn().subscribe({
        next: (res) => {
          this.isWorking = true;
          this.loadingShift = false;
          localStorage.setItem('isWorking', 'true');
          this.cdr.detectChanges(); 
        },
        error: (err) => {
          this.loadingShift = false;
          this.cdr.detectChanges();
          alert("Error al iniciar el turno");
        }
      });
    }
  }

  onInvite() {
    if (!this.inviteEmail) return;

    this.authService.inviteUser(this.inviteEmail, this.tenantId).subscribe({
      next: () => {
        alert(`¡Invitación enviada! Ahora ${this.inviteEmail} tiene acceso.`);
        this.inviteEmail = '';
      },
      error: (err) => {
        console.error(err);
        alert("Error al invitar: " + (err.error?.message || "No tienes permisos."));
      }
    });
  }

  logout() {
    this.authService.logout();
  }
}