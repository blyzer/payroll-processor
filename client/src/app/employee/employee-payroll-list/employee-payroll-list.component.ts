import { DatePipe } from '@angular/common';
import { Component, Input } from '@angular/core';
import { FormControl, FormGroup } from '@angular/forms';

import { faSave } from '@fortawesome/free-solid-svg-icons';
import { ToastrService } from 'ngx-toastr';

import { EmployeePayroll } from '@employee/employee-detail/state/employee-detail.model';

@Component({
  selector: 'app-employee-payroll-list',
  template: ` <h5>Recent Payrolls</h5>
    <ul class="list-group">
      <li
        *ngFor="let payroll of payrolls | slice: 0:9"
        class="list-group-item d-flex justify-content-between payroll-list-item"
        (click)="onSelect(payroll)"
      >
        <ng-container *ngIf="payroll.id !== selectedId; else editable">
          <span> {{ payroll.checkDate | date }} </span>
          <span>
            {{ payroll.grossPayroll | currency }}
          </span>
        </ng-container>
        <ng-template #editable>
          <form [formGroup]="filterForm">
            <div class="row">
              <div class="col-7">
                <input
                  type="date"
                  class="form-control"
                  formControlName="checkDate"
                />
              </div>
              <div class="col-4">
                <input
                  type="text"
                  class="form-control"
                  formControlName="grossPayroll"
                />
              </div>
              <fa-icon
                class="mt-2"
                (click)="update(payroll)"
                [icon]="faSave"
              ></fa-icon>
            </div>
          </form>
        </ng-template>
      </li>
    </ul>`,
  styles: [
    `
      .payroll-list-item {
        background-color: #000;
      }
    `,
  ],
})
export class EmployeePayrollListComponent {
  readonly faSave = faSave;

  filterForm = new FormGroup({
    checkDate: new FormControl(''),
    grossPayroll: new FormControl(''),
  });

  selectedId = '';

  @Input()
  payrolls: EmployeePayroll[];

  constructor(private datePipe: DatePipe, private toastr: ToastrService) {}

  onSelect(payroll: EmployeePayroll) {
    this.filterForm.patchValue({
      ...payroll,
      checkDate: this.datePipe.transform(payroll.checkDate, 'yyyy-MM-dd'),
    });

    this.selectedId = payroll.id;
  }

  update(payroll: EmployeePayroll) {
    // call service here
    this.toastr.error(this.filterForm.get('checkDate').value);
    this.toastr.error(this.filterForm.get('grossPayroll').value.toString());

    this.selectedId = ''; // reset list
  }
}
