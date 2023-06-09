import { Component, Input, OnInit } from '@angular/core';
import { ToastrService } from 'ngx-toastr';
import { Member } from 'src/app/_models/member';
import { MembersService } from 'src/app/_services/members.service';
import { PrescenceService } from 'src/app/_services/prescence.service';

@Component({
  selector: 'app-member-card',
  templateUrl: './member-card.component.html',
  styleUrls: ['./member-card.component.css']
})
export class MemberCardComponent implements OnInit {
  @Input() member: Member | undefined

  constructor(private memberService: MembersService, private toastr: ToastrService, public prescenceService: PrescenceService) { }

  ngOnInit(): void {
  }

  addLike(member: Member){
    this.memberService.addLike(member.userName).subscribe({
        next: () => this.toastr.success(`You have liked ${member.knownAs}`)
    })
  }

}
